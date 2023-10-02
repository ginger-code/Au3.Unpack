module Au3.Unpack.Parsing.CrcTests

open System
open System.IO

open Expecto

open Au3.Unpack

[<Tests>]
let crcTests =
    let testCrc (fileInfo : FileInfo) =
        let name = fileInfo.Name

        testCase $"CRC = {name}"
        <| fun _ ->
            let data = File.ReadAllBytes fileInfo.FullName
            let crc = adler32 (Span.op_Implicit data)

            Expect.equal
                (uint name)
                crc
                $"CRC calculation incorrect, expected '{uint name}' but got '{crc}'"

    let getTestData () =
        let crcDirectory = DirectoryInfo "../../../../../crc_samples/"

        crcDirectory.GetFiles ()

    getTestData ()
    |> List.ofArray
    |> List.map testCrc
    |> testList "CRC"
