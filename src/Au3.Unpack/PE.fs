module Au3.Unpack.PE

open System
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result
open PeNet
open PeNet.Header.Pe

type PEResource = private PEResource of PeFile * ImageResourceDataEntry

module PEFile =
    let getScriptResourceData (data: byte Memory) =
        result {
            let! peFile =
                PeFile.TryParse(data.ToArray())
                |> function
                    | true, peFile -> Ok peFile
                    | _ -> Error "Failed to parse PE file"

            let! rootDirs =
                let dirEntries =
                    [ for entry in peFile.ImageResourceDirectory.DirectoryEntries do
                          if entry.ID = uint ResourceGroupIdType.RcData then
                              entry ]

                if dirEntries.Length = 1 then
                    Ok dirEntries.[0].ResourceDirectory
                elif dirEntries.Length = 0 then
                    Error
                        $"Failed to locate PE resource directory with name '{ResourceGroupIdType.RcData}'"
                else
                    Error
                        $"Multiple PE resource directories were found, automatic resolution failed for name '{ResourceGroupIdType.RcData}'"

            let! resource =
                rootDirs.DirectoryEntries
                |> Seq.tryFind (fun entry -> entry.IsNamedEntry && entry.NameResolved = "SCRIPT")
                |> Option.map (fun entry ->
                    entry.ResourceDirectory.DirectoryEntries.[0].ResourceDataEntry)
                |> Result.requireSome "Failed to resolve script resource"

            let rva = resource.OffsetToData
            let size = resource.Size1 |> int

            let! section =
                peFile.ImageSectionHeaders
                |> Seq.filter (fun section -> section.VirtualAddress < rva)
                |> Seq.sortByDescending (fun section -> section.VirtualAddress)
                |> Seq.tryHead
                |> Result.requireSome "Failed to resolve section header for script resource data"

            let offset = section.PointerToRawData + rva - section.VirtualAddress |> int
            return data.Slice(offset, size + 1).Slice(0x18)
        }
