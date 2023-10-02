open System
open System.IO

open Argu

open Au3.Unpack

[<CliPrefix(CliPrefix.DoubleDash)>]
type Arguments =
    | [<Mandatory ; AltCommandLine("-i")>] InputFile of inputFilePath : string
    | [<Mandatory ; AltCommandLine("-o")>] OutputDirectory of
        outputDirectoryPath : string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | InputFile _ -> "The compiled AutoIt executable to unpack"
            | OutputDirectory _ ->
                "An optional directory to which executable resource contents should be extracted"

let argv = Environment.GetCommandLineArgs () |> Array.skip 1

let errorHandler =
    ProcessExiter (
        colorizer =
            function
            | ErrorCode.HelpText -> None
            | _ -> Some ConsoleColor.Red
    )

let parser =
    ArgumentParser.Create<Arguments> (
        programName = "au3-unpack",
        errorHandler = errorHandler
    )

let arguments = parser.ParseCommandLine (inputs = argv, raiseOnUsage = true)
let inputFile = arguments.GetResult InputFile

let outputDirectory =
    arguments.GetResult OutputDirectory
    |> DirectoryInfo

let unpacked = unpack inputFile

do
    unpacked
    |> Result.iter (
        Array.Parallel.iter (IO.saveDataToFileWithInferredName outputDirectory)
    )

()
