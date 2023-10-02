module Au3.Unpack.IO

open System
open System.IO
open System.Text

open Au3.Unpack.PE

let internal (|ExistingFile|) (filePath : string) =
    let fileInfo = FileInfo filePath

    if not fileInfo.Exists then
        failwith $"Could not find a file at path '{filePath}'"
    else
        fileInfo

let internal determineEncryptionMethod (data : byte Memory) =
    let encryptionMagic =
        "a3484bbe986c4aa9994c530a86d6487d41553321"
        |> Convert.FromHexString

    let data = data.Span

    let index = data.IndexOf encryptionMagic

    match index with
    | -1 -> Ok EncryptionMethod.EA06
    | x ->
        let slice = data.Slice (x + 20, 4)

        if slice.SequenceEqual "EA05"B then
            Error "EA05 encryption not supported"
        elif slice.SequenceEqual "EA06"B then
            Ok EncryptionMethod.EA06
        else
            Error
                $"Unsupported encryption method {Encoding.UTF8.GetString slice}"

let internal retrieveResourceData
    (encryptionMethod : EncryptionMethod)
    (data : byte Memory)
    =
    match encryptionMethod with
    | EA06 -> PEFile.getScriptResourceData data


let saveDataToFileWithInferredName
    (dir : DirectoryInfo)
    (resource : Au3Resource)
    =
    if not dir.Exists then
        dir.CreateSubdirectory "." |> ignore

    match resource.Data with
    | Binary data ->
        let filename =
            resource.Header.Path.Split (
                [| '/' ; '\\' |],
                StringSplitOptions.TrimEntries
                ||| StringSplitOptions.RemoveEmptyEntries
            )
            |> Array.last

        let file = Path.Join (dir.FullName, filename)
        File.WriteAllBytes (file, data.ToArray ())
    | Script text ->
        let filename = $"script_{resource.Header.Crc}.au3"
        let file = Path.Join (dir.FullName, filename)
        File.WriteAllText (file, text)
