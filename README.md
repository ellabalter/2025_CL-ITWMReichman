Our 3D assets were created using a hybrid AI workflow, transforming real-world photos into optimized 3D models using Gemini and Meshy AI. Here is our step-by-step process:

Image Capture: We started by taking a real-world photograph of the target object we wanted to recreate.
Image Refinement (Gemini): We used Gemini to process and refine the photo. Specifically, we prompted the AI to isolate the object and place it on a clean, pure white background to ensure a highly accurate 3D generation.
3D Generation (Meshy AI): We uploaded the refined image to the Meshy AI agent, which interpreted the image and generated the 3D base mesh.
Optimization: Finally, we adjusted and modified the polygon count of the generated mesh to perfectly fit the performance and visual requirements of our project. 

| Asset Description | Website | Asset Link | Project Location |
|---|---|---|---|
| Realistic Palm Tree | Sketchfab | [View asset](https://sketchfab.com/3d-models/realistic-palm-tree-free-39052ea764c945858449e699318efa53) | `Environment/Foliage` |
| Abandoned Billboard | Sketchfab | [View asset](https://sketchfab.com/3d-models/abandoned-billboard-ecf559bac8174154a614a40185104495) | `Environment/Props` |
| Paz Gas Station | Sketchfab | [View asset](https://sketchfab.com/3d-models/israeli-gas-station-paz-d2c37fe91a934bca96ed4d364aebc66d) | `Environment/Buildings` |
| Custom Made Meshy Bus Station | Meshy | N/A | `Environment/Props` |
| Custom Made Meshy Bauhaus | Meshy | N/A | `Environment/Buildings` |
| Custom Made Meshy Herzliya Building | Meshy | N/A | `Environment/Buildings` |
| Colorful Playground | Sketchfab | [View asset](https://sketchfab.com/3d-models/colorful-playground-d453a7658419411aa58c2695f3622cd1) | `Environment/Props` |
| Brutalist Building | Sketchfab | [View asset](https://sketchfab.com/3d-models/brutalist-building-113a8596c74e4ea98955887a6f2d8c1f) | `Environment/Buildings` |
| Soviet Residential Building | Sketchfab | [View asset](https://sketchfab.com/3d-models/soviet-residential-building-3d7f73fcabc044579c477a31ddf4a5b9) | `Environment/Buildings` |
| Ficus Tree | Sketchfab | [View asset](https://sketchfab.com/3d-models/chinese-banyan-ficus-microcarpa-2a0dbcdf8f5d48f5ad79987c7a8170ce) | `Environment/Foliage` |
| Park Bench | Sketchfab | [View asset](https://sketchfab.com/3d-models/park-bench-2ee2bc87756d4bf0932d83ab860ddb8f) | `Environment/Props` |
| Modern Residential Building | Sketchfab | [View asset](https://sketchfab.com/3d-models/residential-complex-modern-apartment-building-d1e54b379c664a349cd4a288527317c8) | `Environment/Buildings` |
| Yoo Towers | Sketchfab | [View asset](https://sketchfab.com/3d-models/yoo-towers-16dbee11a3e348148cc60345a376c8ba) | `Environment/Buildings` |
| Modular Lowpoly Streets | Unity Asset Store | [View asset](https://assetstore.unity.com/packages/3d/environments/urban/modular-lowpoly-streets-free-192094) | `Environment/Roads` |
| Arabic Neoclassical Tall Residential Complex Building | Unity Asset Store | [View asset](https://assetstore.unity.com/packages/3d/environments/urban/arabic-neoclassical-tall-residential-complex-building-317537) | `Environment/Buildings` |
| Residential Complex Modern Apartment Building | Sketchfab | [View asset](https://sketchfab.com/3d-models/residential-complex-modern-apartment-building-1e371673cdac4d9cbf1a5eee2643926d) | Not specified |
| Tower Residential Modern Apartment Building | Sketchfab | [View asset](https://sketchfab.com/3d-models/tower-residential-modern-apartment-building-09dafab2164c4112aab37064166b9fc4) | `Environment/Buildings` |
| Free Low Poly Cars | Unity Asset Store | [View asset](https://assetstore.unity.com/packages/3d/vehicles/mobile-optimize-free-low-poly-cars-327313) | Not specified |
| Curious Cat | Sketchfab | [View asset](https://sketchfab.com/3d-models/curious-cat-3d-model-free-c1465490a7c74d48b599e1d68ff990ef) | Not specified |

## Block System (Procedural Street Randomization)

The street is divided into blocks, each with its own character. There are 3 block types:

- **Playground block** — playground and green park area on the far side of the sidewalk
- **Parking block** — blue/white street parking + off-street parking lot
- **Gas Station block** — gas station + extra billboards and street signs

All blocks always have the same base: road, red/white curb, electricity poles + wires, cats, trees, benches, and a bus stop.

### How to use it

1. Open the scene in Unity and click **StreetManager** in the Hierarchy panel
2. In the Inspector, scroll down to **Block System**
3. Set **Drive Length** — how many blocks you want (e.g. 9 = nine blocks, then the street ends)
4. The system randomly picks one of the 3 block types for each slot, with no two consecutive blocks the same — so 9 blocks could be: Parking → Playground → GasStation → Playground → Parking → GasStation → Playground → Parking → Playground
5. Right-click **Procedural Street (Script)** in the Inspector → **Rebuild Preview** to see the result in the editor
6. To get a different random order, right-click → **Randomize Block Order** (or change **Seed Offset**)
7. **Chunks Per Block** controls how long each block is (1 chunk ≈ 100 m, default 5 = 500 m per block)
8. Press **Play** to drive through the street in first-person — the street hard-stops after the last block

## Pedestrian System

Walking pedestrians are spawned automatically on both sidewalks. Each pedestrian gets a randomly colored shirt (red, blue, green, orange, purple, black, white, or teal).

### How to set up pedestrians (first time)

The character FBX files (`Walking.fbx`, `Walking (1).fbx`) are already in the Assets folder. To wire them up:

1. Click `Walking.fbx` in the Project panel (bottom of Unity)
2. In the Inspector, click the **Rig** tab → set **Animation Type** to **Legacy** → click **Apply**
3. Click the **Animation** tab → click the clip in the list → check **Loop Time** → click **Apply**
4. Repeat steps 2–3 for `Walking (1).fbx`
5. Click **StreetManager** in the Hierarchy
6. In the Inspector, scroll down to **Pedestrian Prefabs**
7. Change **Size** from 0 to **2** and press Enter — two slots appear
8. Drag `Walking.fbx` from the Project panel into **Element 0**
9. Drag `Walking (1).fbx` into **Element 1**
10. Right-click **Procedural Street (Script)** → **Rebuild Preview**, then press **Play**

### To add more character variety (optional)

Download additional characters from **mixamo.com** (free with Adobe account):
- Go to Characters → pick a character → Animations → search "Walking" → check **In Place** → download **FBX for Unity with Skin**
- Import into Unity Assets, set to **Legacy** rig, extract materials
- Add to the **Pedestrian Prefabs** list (increase Size and drag in)

### Inspector settings

| Field | Default | What it does |
|---|---|---|
| Pedestrian Prefabs | — | Drag your Walking FBX files here |
| Pedestrians Per Chunk | 3 | How many pedestrians spawn per ~100m section |
