namespace Au3.Unpack

open System

[<AutoOpen>]
module BitwiseOperators =
    open System

    let xor (data : byte Span) (key : byte Span) =
        let result = Array.zeroCreate data.Length

        for i in 0 .. result.Length - 1 do
            let d = int data.[i]
            let k = int key.[i]
            result.[i] <- d ^^^ k |> byte

        result

    let private (|Mod32|) n = n % 32

    let wrappingRotateLeft (x : uint) (Mod32 n) =
        (x <<< n) ||| (x >>> (32 - n) % 32)


[<RequireQualifiedAccess>]
module String =
    open System

    let decodeUnicode (bytes : byte Span) =
        Text.Encoding.Unicode.GetString bytes

    let escapeString (str : string) =
        let str = str.Replace ("\"", "\"\"")
        $"\"{str}\""

[<AutoOpen>]
module Crc =
    let inline adler32 (source : byte Span) =
        let mutable a = 1UL
        let mutable b = 0UL

        for item in source do
            a <- a + uint64 item
            b <- b + a

        a <- a % 65521UL
        b <- b % 65521UL
        uint32 (b <<< 16 ||| a)

    let crcMatches correctCrc (data : byte Span) =
        let crc = adler32 data
        correctCrc = crc
