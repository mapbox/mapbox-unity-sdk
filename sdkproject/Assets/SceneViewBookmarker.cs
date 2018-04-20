using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneViewBookmarker
{
    struct Bookmark
    {
        public Vector3 pivot;
        public Quaternion rotation;
        public float size;

        public Bookmark(SceneView sceneView)
        {
            pivot = sceneView.pivot;
            rotation = sceneView.rotation;
            size = sceneView.size;
        }

        public void Save(int slot)
        {
            var key = GetKey(slot);
            var json = JsonUtility.ToJson(this);
            EditorPrefs.SetString(key, json);
        }

        public static bool Exists(int slot)
        {
            var key = GetKey(slot);
            return EditorPrefs.HasKey(key);
        }

        public static Bookmark Read(int slot)
        {
            var key = GetKey(slot);
            var json = EditorPrefs.GetString(key);
            return JsonUtility.FromJson<Bookmark>(json);
        }

        static string GetKey(int slot)
        {
            return "sceneViewBookmark" + slot;
        }
    }

    static class BookmarkManager
    {
        const int undoSlot = 0;

        static void BookmarkSceneView(int slot)
        {
            var bookmark = new Bookmark(SceneView.lastActiveSceneView);
            bookmark.Save(slot);

            if (slot != undoSlot)
            {
                Debug.Log("Bookmarked scene view in slot " + slot + ".");
            }
        }

        static void MoveSceneViewToBookmark(int slot)
        {
            // Bookmark the current scene view so that we can easily return to it later.
            if (slot != undoSlot)
            {
                BookmarkSceneView(undoSlot);
            }

            var bookmark = Bookmark.Read(slot);
            SceneView.lastActiveSceneView.MoveToBookmark(bookmark);
        }

        #region Menu Items

        [MenuItem("Window/Scene View Bookmarks/Bookmark Scene View &1", false, 100)]
        static void BookmarkSceneView1() { BookmarkSceneView(1); }

        [MenuItem("Window/Scene View Bookmarks/Bookmark Scene View &2", false, 100)]
        static void BookmarkSceneView2() { BookmarkSceneView(2); }

        [MenuItem("Window/Scene View Bookmarks/Bookmark Scene View &3", false, 100)]
        static void BookmarkSceneView3() { BookmarkSceneView(3); }

        [MenuItem("Window/Scene View Bookmarks/Bookmark Scene View &4", false, 100)]
        static void BookmarkSceneView4() { BookmarkSceneView(4); }

        [MenuItem("Window/Scene View Bookmarks/Bookmark Scene View &5", false, 100)]
        static void BookmarkSceneView5() { BookmarkSceneView(5); }

        [MenuItem("Window/Scene View Bookmarks/Bookmark Scene View &6", false, 100)]
        static void BookmarkSceneView6() { BookmarkSceneView(6); }

        [MenuItem("Window/Scene View Bookmarks/Bookmark Scene View &7", false, 100)]
        static void BookmarkSceneView7() { BookmarkSceneView(7); }

        [MenuItem("Window/Scene View Bookmarks/Bookmark Scene View &8", false, 100)]
        static void BookmarkSceneView8() { BookmarkSceneView(8); }

        [MenuItem("Window/Scene View Bookmarks/Bookmark Scene View &9", false, 100)]
        static void BookmarkSceneView9() { BookmarkSceneView(9); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #1", false, 200)]
        static void MoveSceneViewToBookmark1() { MoveSceneViewToBookmark(1); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #2", false, 200)]
        static void MoveSceneViewToBookmark2() { MoveSceneViewToBookmark(2); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #3", false, 200)]
        static void MoveSceneViewToBookmark3() { MoveSceneViewToBookmark(3); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #4", false, 200)]
        static void MoveSceneViewToBookmark4() { MoveSceneViewToBookmark(4); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #5", false, 200)]
        static void MoveSceneViewToBookmark5() { MoveSceneViewToBookmark(5); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #6", false, 200)]
        static void MoveSceneViewToBookmark6() { MoveSceneViewToBookmark(6); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #7", false, 200)]
        static void MoveSceneViewToBookmark7() { MoveSceneViewToBookmark(7); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #8", false, 200)]
        static void MoveSceneViewToBookmark8() { MoveSceneViewToBookmark(8); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #9", false, 200)]
        static void MoveSceneViewToBookmark9() { MoveSceneViewToBookmark(9); }

        [MenuItem("Window/Scene View Bookmarks/Return To Previous Scene View #0", false, 300)]
        static void ReturnToPreviousSceneView() { MoveSceneViewToBookmark(undoSlot); }

        #endregion

        #region Menu Item Validation

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #1", true)]
        static bool ValidateMoveSceneViewToBookmark1() { return Bookmark.Exists(1); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #2", true)]
        static bool ValidateMoveSceneViewToBookmark2() { return Bookmark.Exists(2); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #3", true)]
        static bool ValidateMoveSceneViewToBookmark3() { return Bookmark.Exists(3); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #4", true)]
        static bool ValidateMoveSceneViewToBookmark4() { return Bookmark.Exists(4); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #5", true)]
        static bool ValidateMoveSceneViewToBookmark5() { return Bookmark.Exists(5); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #6", true)]
        static bool ValidateMoveSceneViewToBookmark6() { return Bookmark.Exists(6); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #7", true)]
        static bool ValidateMoveSceneViewToBookmark7() { return Bookmark.Exists(7); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #8", true)]
        static bool ValidateMoveSceneViewToBookmark8() { return Bookmark.Exists(8); }

        [MenuItem("Window/Scene View Bookmarks/Move Scene View To Bookmark #9", true)]
        static bool ValidateMoveSceneViewToBookmark9() { return Bookmark.Exists(9); }

        [MenuItem("Window/Scene View Bookmarks/Return To Previous Scene View #0", true)]
        static bool ValidateReturnToPreviousSceneView() { return Bookmark.Exists(undoSlot); }

        #endregion
    }

    static class SceneViewExtensions
    {
        public static void MoveToBookmark(this SceneView sceneView, Bookmark bookmark)
        {
            sceneView.pivot = bookmark.pivot;
            sceneView.rotation = bookmark.rotation;
            sceneView.size = bookmark.size;
        }
    }
}
