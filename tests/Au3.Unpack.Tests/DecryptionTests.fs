module Au3.Unpack.DecryptionTests

open System
open System.IO

open Expecto

open Au3.Unpack

[<Tests>]
let ea06DecryptionTests =
    let testLame
        (algorithm : int -> int -> byte array)
        algorithmName
        (lameValuesFile : FileInfo)
        =
        let name = lameValuesFile.Name
        let seed = int name
        let correct = File.ReadAllBytes lameValuesFile.FullName
        let length = correct.Length

        testCase
            $"Generating LAME encryption values using {algorithmName} for seed '{seed}'"
        <| fun _ ->
            let generated = algorithm seed length
            let diff = ResizeArray ()

            for i in 0 .. generated.Length - 1 do
                let g = generated[i]
                let a = correct[i]

                if g <> a then
                    diff.Add (i, g, a)

            Expect.hasLength
                diff
                0
                $"Generated LAME sequence contained errors: %A{diff}"

    let getLameData () =
        let lameDataDirectory =
            DirectoryInfo "../../../../../encryption_samples/lame/"

        lameDataDirectory.GetFiles () |> List.ofArray

    let lameTests algorithm algorithmName =
        getLameData ()
        |> List.map (testLame algorithm algorithmName)

    let testDecryption (encryptedFile : FileInfo, decryptedFile : FileInfo) =
        let name = encryptedFile.Name

        testCase $"Decrypting data should match correct sample for {name}"
        <| fun _ ->
            let encrypted = File.ReadAllBytes encryptedFile.FullName
            let correct = File.ReadAllBytes decryptedFile.FullName
            let key = EncryptionMethod.resourceContentMagic EA06
            Decryption.decryptInPlace EA06 key (encrypted.AsSpan ())
            let mutable incorrectIndices = []

            for i in 0 .. correct.Length - 1 do
                if correct[i] <> encrypted[i] then
                    incorrectIndices <- i :: incorrectIndices

            match incorrectIndices with
            | [] -> ()
            | indices ->
                failtest
                    $"Data is incorrect at the following indices: %A{indices |> List.sort}"

    let getDecryptionTestData () =
        let encryptedDirectory =
            DirectoryInfo "../../../../../encryption_samples/encrypted/"

        let decryptedDirectory =
            DirectoryInfo "../../../../../encryption_samples/decrypted/"

        let encrypted =
            encryptedDirectory.GetFiles ()
            |> Array.sortBy (fun file -> file.Name)

        let decrypted =
            decryptedDirectory.GetFiles ()
            |> Array.sortBy (fun file -> file.Name)

        Array.zip encrypted decrypted |> List.ofArray

    let decryptionTests =
        [
            yield
                testCase "Basic data decryption"
                <| fun _ ->
                    let data =
                        [|
                            107uy
                            67uy
                            202uy
                            82uy
                        |]

                    let decrypted = Decryption.ea06Decrypt (data.AsSpan ()) 6382

                    let expected =
                        [|
                            70uy
                            73uy
                            76uy
                            69uy
                        |]

                    Expect.equal decrypted expected "EA06 decryption failed"
            yield!
                (getDecryptionTestData ()
                 |> List.map testDecryption)
        ]

    testList
        "EA06 decryption"
        [
            testList
                "LAME algorithm"
                (nameof PRNG.LAME.lameN
                 |> lameTests PRNG.LAME.lameN)
            testList "Decryption" decryptionTests
        ]
