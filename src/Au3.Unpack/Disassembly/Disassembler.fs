module Au3.Unpack.Disassembly.Disassembler

open System
open System.Text

open Au3.Unpack
open Au3.Unpack.Collections

module Literals =
    open FSharp.Data.LiteralProviders
    open System.IO

    let keywords =
        TextFile.Disassembly.LiteralData.keywords.Path
        |> File.ReadAllLines

    let functions =
        TextFile.Disassembly.LiteralData.functions.Path
        |> File.ReadAllLines

    let macros =
        TextFile.Disassembly.LiteralData.macros.Path
        |> File.ReadAllLines

    let keywordLookup =
        keywords
        |> Array.map (fun s -> s.ToUpper (), s)
        |> Map.ofArray

    let functionLookup =
        functions
        |> Array.map (fun s -> s.ToUpper (), s)
        |> Map.ofArray

    let macroLookup =
        macros
        |> Array.map (fun s -> s.ToUpper (), s)
        |> Map.ofArray

let private getXorString (stream : ByteStream) =
    let key = stream.ReadU32 ()

    if not (stream.OffsetInBounds key) then
        failwith "XORed string is out of bounds"

    let ret : byte array = Array.zeroCreate (int key * 2)

    for i in 0 .. int key - 1 do
        let c = stream.ReadU16 () ^^^ uint16 key
        ret[i * 2 + 0] <- byte c &&& 0xFFuy
        ret[i * 2 + 1] <- byte (c >>> 8) &&& 0xFFuy

    Encoding.Unicode.GetString ret


let inline private applyKeywordIndent
    clearNextIndent
    incrementNextIndent
    decrementNextIndent
    decrementIndent
    (next : byte ValueOption)
    keyword
    =
    if
        List.contains
            keyword
            [
                "While"
                "Do"
                "For"
                "Select"
                "Switch"
                "Func"
                "If"
            ]
    then
        incrementNextIndent ()

    if List.contains keyword [ "Case" ; "Else" ; "ElseIf" ] then
        decrementIndent ()

    if
        List.contains
            keyword
            [
                "WEnd"
                "Until"
                "Next"
                "EndSelect"
                "EndSwitch"
                "EndFunc"
                "EndIf"
            ]
    then
        decrementIndent ()
        incrementNextIndent ()

    if
        List.contains keyword [ "Then" ]
        && next <> ValueSome 0x7Fuy
    then
        decrementNextIndent ()

    if List.contains keyword [ "EndFunc" ] then
        clearNextIndent ()

let inline private readKeywordId
    clearNextIndent
    incrementNextIndent
    decrementNextIndent
    decrementIndent
    (stream : ByteStream)
    =
    let keywordNo = stream.ReadI32 ()

    if keywordNo > Literals.keywords.Length then
        failwith "Token not found in keywords"

    let keyword = Literals.keywords[keywordNo]
    let next = stream.PeekNextByte ()

    applyKeywordIndent
        clearNextIndent
        incrementNextIndent
        decrementNextIndent
        decrementIndent
        next
        keyword

    keyword

let inline private readKeyword
    clearNextIndent
    incrementNextIndent
    decrementNextIndent
    decrementIndent
    (stream : ByteStream)
    =
    let keyword = Literals.keywordLookup[getXorString stream]
    let next = stream.PeekNextByte ()

    applyKeywordIndent
        clearNextIndent
        incrementNextIndent
        decrementNextIndent
        decrementIndent
        next
        keyword

    keyword

let inline private readFunctionId (stream : ByteStream) =
    let functionNo = stream.ReadI32 ()

    if functionNo > Literals.keywords.Length then
        failwith "Token not found in functions"

    Literals.functions[functionNo]

let inline private readFunction (stream : ByteStream) =
    let xorString = getXorString stream
    Literals.functionLookup[xorString]

let inline private readMacro (stream : ByteStream) =
    let xorString = getXorString stream
    "@" + Literals.macroLookup[xorString]

let internal disassemble (data : byte Memory) (indentLines : bool) =
    let mutable stream = ByteStream.createFrom data
    let lineCount = stream.ReadU32 () |> int
    let mutable currentLine = 0
    let mutable indent = 0
    let mutable nextIndent = 0
    let pushNextIndent () = indent <- nextIndent
    let clearNextIndent () = nextIndent <- 0
    let incrementNextIndent () = nextIndent <- nextIndent + 1
    let decrementNextIndent () = nextIndent <- nextIndent - 1
    let decrementIndent () = indent <- indent - 1

    let indentString =
        if indentLines then
            "    "
        else
            String.Empty

    let mutable result = Array.zeroCreate lineCount
    let mutable lineItems = ResizeArray ()

    while currentLine < lineCount do
        let opCode = int <| stream.ReadU8 ()

        if opCode = 0x7F then

            let line =
                (String.replicate indent indentString)
                + (lineItems |> String.concat " ")

            result.[currentLine] <- line
            currentLine <- currentLine + 1
            lineItems <- ResizeArray ()
            pushNextIndent ()

        else
            lineItems.Add (
                match opCode with
                // Keyword
                | 0x00 ->
                    readKeywordId
                        clearNextIndent
                        incrementNextIndent
                        decrementNextIndent
                        decrementIndent
                        stream
                // Function
                | 0x01 -> readFunctionId stream
                // Numbers
                | 0x05 -> stream.ReadU32 () |> string
                | 0x10 -> stream.ReadU64 () |> string
                | 0x20 -> stream.ReadF64 () |> string
                // Statements
                | 0x30 ->
                    readKeyword
                        clearNextIndent
                        incrementNextIndent
                        decrementNextIndent
                        decrementIndent
                        stream
                | 0x31 -> readFunction stream
                | 0x32 -> readMacro stream
                | 0x33 -> "$" + getXorString stream
                | 0x34 -> getXorString stream
                | 0x35 -> "." + getXorString stream
                | 0x36 -> String.escapeString (getXorString stream)
                | 0x37 -> getXorString stream
                // Operators
                | 0x40 -> ","
                | 0x41 -> "="
                | 0x42 -> ">"
                | 0x43 -> "<"
                | 0x44 -> "<>"
                | 0x45 -> ">="
                | 0x46 -> "<="
                | 0x47 -> "("
                | 0x48 -> ")"
                | 0x49 -> "+"
                | 0x4A -> "-"
                | 0x4B -> "/"
                | 0x4C -> "*"
                | 0x4D -> "&"
                | 0x4E -> "["
                | 0x4F -> "]"
                | 0x50 -> "=="
                | 0x51 -> "^"
                | 0x52 -> "+="
                | 0x53 -> "-="
                | 0x54 -> "/="
                | 0x55 -> "*="
                | 0x56 -> "&="
                | 0x57 -> "?"
                | 0x58 -> ":"
                | _ -> failwith $"Unsupported opcode: {opCode:X}"
            )

    result |> String.concat Environment.NewLine

let internal disassembleHeaderData (header : Au3Resource'') =
    {
        Header = header.Header
        Data =
            match header.Data with
            | Compiled compiledData -> disassemble compiledData true |> Script
            | Raw rawData -> Binary rawData
            | Text textData -> Script textData
    }
