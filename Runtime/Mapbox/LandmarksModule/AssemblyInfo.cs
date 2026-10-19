using System.Runtime.CompilerServices;

// LandmarkPolygonFilter is internal - it is reached only through LandmarkPolygonFilterObject - but
// its tile-edge replication is subtle enough to want direct tests, which live in the editor test
// assembly.
[assembly: InternalsVisibleTo("LandmarksModuleTests")]
