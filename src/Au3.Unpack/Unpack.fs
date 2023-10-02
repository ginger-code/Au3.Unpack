[<AutoOpen>]
module Au3.Unpack.Unpack

open System
open System.IO

open FsToolkit.ErrorHandling

open Au3.Unpack.Disassembly
open Au3.Unpack.Decompression
open Au3.Unpack.IO

let unpack (ExistingFile file) =
    result {
        let fileData = File.ReadAllBytes file.FullName
        let fileDataMemory = fileData.AsMemory ()
        let! encryptionMethod = determineEncryptionMethod fileDataMemory
        let! resourceData = retrieveResourceData encryptionMethod fileDataMemory
        let resources = Scanning.parseAllResources encryptionMethod resourceData

        return
            resources
            |> Array.Parallel.map (
                Decompression.decompressResourceData
                >> Disassembler.disassembleHeaderData
            )
    }
