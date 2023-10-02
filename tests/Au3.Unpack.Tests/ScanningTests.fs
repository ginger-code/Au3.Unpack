module Au3.Unpack.Parsing.ScanningTests

open System

open Expecto

open Au3.Unpack.Collections
open Au3.Unpack.Decryption
open Au3.Unpack

[<Tests>]
let scanningTests =
    testList
        "Scanning"
        [

          testCase "readDate"
          <| fun _ ->
              let expected = DateTime(2023, 6, 7, 9, 22, 28)
              let high = 31037763
              let low = 457070825

              let bytes =
                  [| yield! BitConverter.GetBytes high
                     yield! BitConverter.GetBytes low |]

              let mutable stream = ByteStream.createFrom (bytes.AsMemory())
              let result = Scanning.readDate stream
              Expect.equal result.Year expected.Year "Parsed date was incorrect"
              Expect.equal result.Month expected.Month "Parsed date was incorrect"
              Expect.equal result.Day expected.Day "Parsed date was incorrect"
              Expect.equal result.Hour expected.Hour "Parsed date was incorrect"
              Expect.equal result.Minute expected.Minute "Parsed date was incorrect"
              Expect.equal result.Second expected.Second "Parsed date was incorrect"

          testCase "readString"
          <| fun _ ->
              let expected = ">>>AUTOIT NO CMDEXECUTE<<<"

              let bytes =
                  [| 0xa6uy
                     0xaduy
                     0x0uy
                     0x0uy
                     0xe1uy
                     0xbbuy
                     0x3auy
                     0x21uy
                     0xa5uy
                     0x29uy
                     0xe3uy
                     0xecuy
                     0xe7uy
                     0xbuy
                     0x98uy
                     0x2euy
                     0x40uy
                     0xbduy
                     0xe1uy
                     0x9auy
                     0xdeuy
                     0x80uy
                     0x46uy
                     0xb1uy
                     0x9duy
                     0x6buy
                     0x3buy
                     0x21uy
                     0xd4uy
                     0xb1uy
                     0xd6uy
                     0x75uy
                     0x3auy
                     0xc8uy
                     0x3duy
                     0xc6uy
                     0xd0uy
                     0x33uy
                     0xf7uy
                     0x14uy
                     0xafuy
                     0xcbuy
                     0x17uy
                     0xa2uy
                     0x94uy
                     0x1uy
                     0x8duy
                     0x13uy
                     0x88uy
                     0xfeuy
                     0x64uy
                     0x95uy
                     0x61uy
                     0xe7uy
                     0xb6uy
                     0x4duy |]

              let mutable stream = ByteStream.createFrom (bytes.AsMemory())
              let encryptionMethod = EncryptionMethod.EA06
              let result = Scanning.readSubtype encryptionMethod stream
              Expect.equal result expected "Failed to decode string from encrypted data stream"

          ]
