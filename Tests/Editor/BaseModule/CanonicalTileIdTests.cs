using Mapbox.BaseModule.Data.Tiles;
using NUnit.Framework;

namespace Mapbox.BaseModuleTests
{
    public class CanonicalTileIdTests
    {
        [Test]
        public void ParentAt_LeavesTheSourceTileUnchanged()
        {
            var id = new CanonicalTileId(16, 10485, 25326);

            var parent = id.ParentAt(14);

            Assert.AreEqual(new CanonicalTileId(16, 10485, 25326), id);
            Assert.AreEqual(new CanonicalTileId(14, 2621, 6331), parent);
        }

        [Test]
        public void ParentAt_MatchesRepeatedMoveToParent()
        {
            var expected = new CanonicalTileId(16, 10485, 25326);
            expected.MoveToParent();
            expected.MoveToParent();

            Assert.AreEqual(expected, new CanonicalTileId(16, 10485, 25326).ParentAt(14));
        }

        [Test]
        public void ParentAt_OwnZoomReturnsItself()
        {
            var id = new CanonicalTileId(14, 2621, 6331);

            Assert.AreEqual(id, id.ParentAt(14));
        }

        [Test]
        public void ParentAt_ZoomBelowOwnReturnsItselfRatherThanDescending()
        {
            var id = new CanonicalTileId(12, 655, 1582);

            Assert.AreEqual(id, id.ParentAt(15));
        }

        [Test]
        public void ParentAt_WalksAllTheWayToTheRootTile()
        {
            var id = new CanonicalTileId(16, 10485, 25326);

            Assert.AreEqual(new CanonicalTileId(0, 0, 0), id.ParentAt(0));
        }

        [Test]
        public void IsParentOf_DoesNotMutateEitherTile()
        {
            var parent = new CanonicalTileId(14, 2621, 6331);
            var child = new CanonicalTileId(16, 10485, 25326);

            Assert.IsTrue(parent.IsParentOf(child));
            Assert.AreEqual(new CanonicalTileId(14, 2621, 6331), parent);
            Assert.AreEqual(new CanonicalTileId(16, 10485, 25326), child);
        }

        [Test]
        public void IsParentOf_RejectsAnUnrelatedTileAtTheSameZoom()
        {
            var parent = new CanonicalTileId(14, 2621, 6331);
            var stranger = new CanonicalTileId(16, 10489, 25326);

            Assert.IsFalse(parent.IsParentOf(stranger));
        }
    }
}
