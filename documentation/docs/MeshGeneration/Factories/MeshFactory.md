# Mesh Factory

Factory responsible for the visualization of vector data.

##Layer Visualizers

####[Vector Layer Visualizer](VectorLayerVisualizer.md)

Creates visualization for polygon and line features in 3D space.

####[Poi Layer Visualizer](PoiLayerVisualizer.md)

Creates visualization for point features in 2D/3D space.

##Mesh Modifiers

Mesh Modifiers creates the data, like vertices, triangles, normals and UVs, to build meshes.

####[Chamfer Modifier](Modifiers/ChamferModifier.md)

Bevels corners and adds another vertex&line to craete a smoother corner. Work only with Polygon Mesh Modifier and used to create smooth building meshes.

####[Polygon Mesh Modifier](Modifiers/PolygonMeshModifier.md)

Creates polygon from a vertices of a polygon feature. Main used for creating building footprint, landuse, water etc meshes.

####[Line Mesh Modifier](Modifiers/LineMeshModifier.md)

Creates polygons from a vertices of a line feature. Mainly used for creating road, border, contour line meshes.

####[Height Modifier](Modifiers/HeightModifier.md)

Pushes a polygon up depending on the features height value and creates side walls down to features minimum height value or ground.

####[UV Modifier](Modifiers/UvModifier.md)

Creates UV map values for polygons. Works with Polygon Mesh Modifier.

##Game Object Modifiers

Game Object Modifiers works on and modifies an already existing game object with a mesh.

####[Texture Modifier](Modifiers/TextureModifier.md)

Changes the given game object's material.