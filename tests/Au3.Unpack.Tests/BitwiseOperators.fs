module Tests

open System

open Expecto

open Au3.Unpack

let xorTests =
    testList
        "xor"
        [ testCase "data = key"
          <| fun _ ->
              let data = [| 1uy ; 2uy ; 3uy |]
              let key = [| 1uy ; 2uy ; 3uy |]
              let result = xor (data.AsSpan()) (key.AsSpan())
              Expect.equal result [| 0uy ; 0uy ; 0uy |] "Failed to xor [| 1; 2; 3 |] with itself"

          testCase "data.Length = 1; key.Length = 3"
          <| fun _ ->
              let data = [| 6uy |]
              let key = [| 5uy ; 7uy ; 9uy |]
              let result = xor (data.AsSpan()) (key.AsSpan())
              Expect.equal result [| 3uy |] "Failed to xor [| 6 |] with [| 5; 7; 9 |]" ]


let rotateTests =
    let cases =
        [ uint 1, 1, uint 2
          uint 1, 2, uint 4
          uint 1, 3, uint 8
          uint 1, 4, uint 16
          uint 1, 5, uint 32
          uint 1, 30, uint 1073741824
          uint 1, 32, uint 1
          uint 2, 1, uint 4
          uint 2, 2, uint 8
          uint 2, 3, uint 16
          uint 2, 31, uint 1
          uint 2, 32, uint 2 ]

    let test (x, y, expected) =
        testCase $"rol {x} {y} = {expected}"
        <| fun _ ->
            let result = wrappingRotateLeft x y
            Expect.equal result expected "Wrapping rotate left incorrect"

    cases |> List.map test |> testList "wrappingRotateLeft"




[<Tests>]
let tests = testList "BitwiseOperators" [ xorTests ; rotateTests ]
