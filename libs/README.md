# To update these libraries
## Assembly-CSharp_publicized.dll
1. Download and extract [CabbageCrow/AssemblyPublicizer](https://github.com/CabbageCrow/AssemblyPublicizer)
2. Download `mono-cil-strip`  
  - For Windows developers, I have no clue how this is done. Please add it if you ever figure it out.  
  - For Linux developers, this may be present in your distribution's Mono package  
    - Arch Linux: `mono`
3. Drag the original `Assembly-CSharp.dll` onto the publicizer, then drag the publicized result onto `mono-cil-strip`
## Assembly-CSharp-firstpass.dll
1. Drag the assembly onto `mono-cil-strip`
