Imports System.IO

Public Interface iNotifyShellUpdate
    Property BibliothequeLiee As tagID3Bibliotheque
    Property Filter As String
    Sub NotifyShellWatcherFilesCreated(ByVal e As FileSystemEventArgs, ByVal Infos As tagID3FilesInfos)
    Sub NotifyShellWatcherFilesDeleted(ByVal e As FileSystemEventArgs)
    Sub NotifyShellWatcherFilesChanged(ByVal e As FileSystemEventArgs, ByVal Infos As tagID3FilesInfos)
    Sub NotifyShellWatcherFilesRenamed(ByVal e As RenamedEventArgs, ByVal Infos As tagID3FilesInfos)
End Interface
