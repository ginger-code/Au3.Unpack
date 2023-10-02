module Au3.Unpack.Collections.BitStreamTests

open Expecto

open Au3.Unpack.Collections


[<Tests>]
let bitStreamTests =
    testList
        "BitStream"
        [ testCase "from byte"
          <| fun _ ->
              let mutable stream = BitStream([| 0xFuy |])
              let bits = stream.GetBits 8
              Expect.equal bits 0xF "BitStream returned incorrect bits"

          testCase "from bytes"
          <| fun _ ->
              let mutable stream = BitStream([| 0xFuy ; 0xCuy ; 0b00001100uy |])
              let bits = stream.GetBits 8
              Expect.equal bits 0xF "BitStream returned incorrect bits"
              let bits = stream.GetBits 8
              Expect.equal bits 0xC "BitStream returned incorrect bits"

              for b in [ 0 ; 0 ; 0 ; 0 ; 1 ; 1 ; 0 ; 0 ] do
                  let bit = stream.GetBits 1
                  Expect.equal bit b "BitStream returned incorrect bit"

          testCase "from bits"
          <| fun _ ->
              let mutable stream = BitStream([| 0b00011111uy |])
              let bits = stream.GetBits 8
              Expect.equal bits 0b00011111 "BitStream returned incorrect bits"

          testCase "from bits (truncated)"
          <| fun _ ->
              let mutable stream = BitStream([| 0b00011111uy |])
              let bits = stream.GetBits 4
              Expect.equal bits 0b0001 "BitStream returned incorrect bits"
              let bits = stream.GetBits 4
              Expect.equal bits 0b1111 "BitStream returned incorrect bits"


          ]
