// Unity mounts Application.persistentDataPath on an IndexedDB-backed filesystem in
// Web builds, but a File.WriteAllText only lands in the in-memory layer. Nothing
// reaches IndexedDB until FS.syncfs runs, and Unity does not call it for arbitrary
// file writes, so without this the save file is silently lost when the tab closes.
mergeInto(LibraryManager.library, {
  CyberStationSyncFileSystem: function () {
    if (typeof FS === "undefined" || typeof FS.syncfs !== "function") {
      return;
    }

    // populate: false means "push the in-memory state out to IndexedDB".
    FS.syncfs(false, function (error) {
      if (error) {
        console.error("Cyber Station: could not flush save data to IndexedDB", error);
      }
    });
  },
});
