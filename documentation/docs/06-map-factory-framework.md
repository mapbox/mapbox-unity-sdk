# Map Factory Framework

This framework allows you to create and style your maps through factory and visualization abstractions. The system is intended to be designer-friendly by serializing configuration settings in `ScriptableObjects`.

## Map Controller

Not a part of the Map Visualization structure but respresents the rest of the application and requests tiles from Map Visualization.

## Map Visualization

Core class of the visualization tree, keeps and manages the factories.

###Factories

Factories are wrappers around Mapbox Api end points like raster image or vector data.

####[Terrain Factory](MeshGeneration/Factories/TerrainFactory.md)

Factory responsible for creating the world base.

####[Map Image Factory](MeshGeneration/Factories/MapImageFactory.md)

Factory responsible for assigning materials and textures to the world base mesh.

####[Mesh Factory](MeshGeneration/Factories/MeshFactory.md)

Factory responsible for the visualization of vector data.