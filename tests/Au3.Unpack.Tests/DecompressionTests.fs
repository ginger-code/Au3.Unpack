module Au3.Unpack.Parsing.DecompressionTests

open System
open System.IO

open Au3.Unpack
open Expecto

open Au3.Unpack.Collections
open Au3.Unpack.Decompression.Decompression

[<Tests>]
let numberOfBytesToCopyTests =
    testList
        "numberOfBytesToCopy"
        [
            testCase "[| 0; 1; 2; 3; 4; 5; 6; 7; 8; 9 |]"
            <| fun _ ->
                let mutable stream =
                    BitStream (
                        [|
                            1uy
                            2uy
                            3uy
                            4uy
                            5uy
                            6uy
                            7uy
                            8uy
                            9uy
                        |]
                    )

                let result = numberOfBytesToCopy stream
                Expect.equal result 3 "Match length should be 3"

            testCase "[| 7; 1; 1; 1 |]"
            <| fun _ ->
                let mutable stream =
                    BitStream (
                        [|
                            7uy
                            1uy
                            1uy
                            1uy
                        |]
                    )

                let _ = stream.GetBits 5
                let result = numberOfBytesToCopy stream
                Expect.equal result 10 "Match length should be 10"
        ]


[<Tests>]
let decompressionTests =
    let testDecompression (compressedFilePath, decompressedFilePath) =
        let name = FileInfo(compressedFilePath).Name

        testCase $"Decompress {name}"
        <| fun _ ->
            let compressed = File.ReadAllBytes compressedFilePath

            let compressedSpan = compressed.AsMemory ()

            let correct = File.ReadAllBytes decompressedFilePath

            let buffer =
                decompressData
                    EncryptionMethod.EA06
                    correct.Length
                    compressedSpan.Span

            Expect.sequenceEqual
                (buffer.ToArray ())
                correct
                "Decompressed data was incorrect"

    let getTestData () =
        let compressedDirectory =
            DirectoryInfo "../../../../../compression_samples/compressed"

        let decompressedDirectory =
            DirectoryInfo "../../../../../compression_samples/decompressed"

        let compressedFiles =
            compressedDirectory.GetFiles ()
            |> Array.map (fun file -> file.FullName)
            |> Array.sort

        let decompressedFiles =
            decompressedDirectory.GetFiles ()
            |> Array.map (fun file -> file.FullName)
            |> Array.sort

        decompressedFiles
        |> Array.zip compressedFiles
        |> List.ofArray

    testList "decompress" (getTestData () |> List.map testDecompression)
