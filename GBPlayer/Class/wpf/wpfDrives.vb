'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 31/12/11
'DESCRIPTION : Classe d'acces aux disques du système
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.Windows.Interop
Imports System.Management
Imports System.Collections.ObjectModel
Imports System.IO

Public Class wpfDrives
    Implements IDisposable
    '***********************************************************************************************
    '----------------------------------ENUMERATION DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Public Enum EnumDriveUpdate
        Null = &H0
        CDRomModification = &H1
        DiskAdd = &H10
        DiskDel = &H100
    End Enum
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Public Event DiskAdded(ByVal Name As String)
    Public Event DiskRemoved(ByVal Name As String)
    Public Event DiskCDRomEjected(ByVal Name As String)
    Public Event DiskCDRomInsered(ByVal Name As String)
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Private WatcherCDROM As ManagementEventWatcher = Nothing 'Permet de surveiller l'insertion et l'éjection de CDROM
    Private WatcherAddDisk As ManagementEventWatcher = Nothing 'Permet de surveiller l'insertion et l'éjection de CDROM
    Private WatcherDelDisk As ManagementEventWatcher = Nothing 'Permet de surveiller l'insertion et l'éjection de CDROM
    Private DriveAdded As String
    Private DriveRemoved As String
    Public Property DiskCollection As ObservableCollection(Of String) = New ObservableCollection(Of String)
    Private Delegate Sub NoArgDelegate()
    '***********************************************************************************************
    '----------------------------------CONSTRUCTEUR DE LA CLASSE------------------------------------
    '***********************************************************************************************
    Public Sub New(ByVal flags As EnumDriveUpdate)
        SetWatcherDrives(flags)
    End Sub

    '***********************************************************************************************
    '----------------------------------DESTRUCTEUR DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Private disposedValue As Boolean ' Pour détecter les appels redondants
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                If WatcherCDROM IsNot Nothing Then WatcherCDROM.Stop()
                If WatcherAddDisk IsNot Nothing Then WatcherAddDisk.Stop()
                If WatcherDelDisk IsNot Nothing Then WatcherDelDisk.Stop()
            End If
        End If
        Me.disposedValue = True
    End Sub

    '***********************************************************************************************
    '----------------------------------PROCEDURES PUBLIQUES-----------------------------------------
    '***********************************************************************************************
    Public Sub AddWatcher(ByVal flags As EnumDriveUpdate)
        SetWatcherDrives(flags)
    End Sub
    Public Function GetCDRomDrives() As List(Of String)
        Dim CollectionTemp As List(Of String) = New List(Of String)
        For Each i In FileIO.FileSystem.Drives
            If i.DriveType = DriveType.CDRom Then CollectionTemp.Add(i.Name)
        Next
        Return CollectionTemp
    End Function
    Public Shared Function IsCDRomDrive(ByVal Name) As Boolean
        For Each i In FileIO.FileSystem.Drives
            If (i.DriveType = DriveType.CDRom) And (Path.GetPathRoot(Name) = i.Name) Then
                Return True
            End If
        Next
        Return False
    End Function
    '***********************************************************************************************
    '----------------------------------PROCEDURES PRIVEES-------------------------------------------
    '***********************************************************************************************
    Private Sub SetWatcherDrives(ByVal Flags As EnumDriveUpdate)
        'Initialisation de la surveillance de l'insertion de CDROM
        Dim opt As ConnectionOptions = New ConnectionOptions
        opt.EnablePrivileges = True 'sets required privilege
        Try
            Dim scope As New ManagementScope("root\CIMV2", opt)
            UpdateSystemDisk()
            If Flags = EnumDriveUpdate.Null Then Exit Sub
            If (WatcherCDROM Is Nothing) And
                        ((Flags And EnumDriveUpdate.CDRomModification) = EnumDriveUpdate.CDRomModification) Then
                Dim WatcherRequeteCDROM As WqlEventQuery
                WatcherRequeteCDROM = New WqlEventQuery
                WatcherRequeteCDROM.EventClassName = "__InstanceModificationEvent"
                WatcherRequeteCDROM.WithinInterval = New TimeSpan(0, 0, 1)
                WatcherRequeteCDROM.Condition = "TargetInstance ISA 'Win32_LogicalDisk' and TargetInstance.DriveType = 5"
                WatcherCDROM = New ManagementEventWatcher(scope, WatcherRequeteCDROM)
                AddHandler WatcherCDROM.EventArrived, AddressOf CDREventArrived
                WatcherCDROM.Start()
            End If
            If (WatcherAddDisk Is Nothing) And
                        ((Flags And EnumDriveUpdate.DiskAdd) = EnumDriveUpdate.DiskAdd) Then
                Dim WatcherRequeteAddDisk As WqlEventQuery
                WatcherRequeteAddDisk = New WqlEventQuery
                WatcherRequeteAddDisk.EventClassName = "__InstanceCreationEvent"
                WatcherRequeteAddDisk.WithinInterval = New TimeSpan(0, 0, 3)
                WatcherRequeteAddDisk.Condition = "TargetInstance ISA 'Win32_LogicalDisk'" ''Win32_USBControllerdevice'" '  and TargetInstance.VolumeName != null"
                WatcherAddDisk = New ManagementEventWatcher(scope, WatcherRequeteAddDisk)
                AddHandler WatcherAddDisk.EventArrived, AddressOf DISKEventArrivee
                WatcherAddDisk.Start()
            End If
            If (WatcherDelDisk Is Nothing) And
                        ((Flags And EnumDriveUpdate.DiskDel) = EnumDriveUpdate.DiskDel) Then
                Dim WatcherRequeteDelDisk As WqlEventQuery
                WatcherRequeteDelDisk = New WqlEventQuery
                WatcherRequeteDelDisk.EventClassName = "__InstanceDeletionEvent"
                WatcherRequeteDelDisk.WithinInterval = New TimeSpan(0, 0, 3)
                WatcherRequeteDelDisk.Condition = "TargetInstance ISA 'Win32_LogicalDisk'" ''Win32_USBControllerdevice'" '  and TargetInstance.VolumeName != null"
                WatcherDelDisk = New ManagementEventWatcher(scope, WatcherRequeteDelDisk)
                AddHandler WatcherDelDisk.EventArrived, AddressOf DISKEventArrivee
                WatcherDelDisk.Start()
            End If
            'Win32_USBController' pour les clé USB
            'Win32_USBControllerDevice'
        Catch ex As Exception
            MsgBox("erreur init liste disque")
        End Try

    End Sub
    '-----PROCEDURE DE TRAITEMENT EVENEMENT INSERTION ET EJECTION CDROM
    Private Sub CDREventArrived(ByVal sender As Object, ByVal e As EventArrivedEventArgs)
        Dim pd As PropertyData = e.NewEvent.Properties("TargetInstance")
        If (pd IsNot Nothing) Then
            UpdateSystemDisk()
            Dim mbo As ManagementBaseObject = CType(pd.Value, ManagementBaseObject)
            If mbo.Properties("VolumeName").Value IsNot Nothing Then
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send,
                                          New NoArgDelegate(Sub()
                                                                RaiseEvent DiskCDRomInsered(DriveRemoved)
                                                            End Sub))
            Else
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send,
                                          New NoArgDelegate(Sub()
                                                                RaiseEvent DiskCDRomEjected(DriveRemoved)
                                                            End Sub))
            End If
        End If
    End Sub
    '-----PROCEDURE DE TRAITEMENT EVENEMENT AJOUT OU SUPPRESSION DISQUE
    Private Sub DISKEventArrivee(ByVal sender As Object, ByVal e As EventArrivedEventArgs)
        Dim pd As PropertyData = e.NewEvent.Properties("TargetInstance")
        If (pd IsNot Nothing) Then
            UpdateSystemDisk()
            Dim mbo As ManagementBaseObject = CType(pd.Value, ManagementBaseObject)
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send,
                          New NoArgDelegate(Sub()
                                                If DriveAdded <> "" Then RaiseEvent DiskAdded(DriveAdded)
                                                If DriveRemoved <> "" Then RaiseEvent DiskRemoved(DriveRemoved)
                                            End Sub))
        End If
    End Sub
    Private Sub UpdateSystemDisk()
        Dim CollectionTemp As ObservableCollection(Of String) = New ObservableCollection(Of String)
        Dim CopieNewCollection As ObservableCollection(Of String) = New ObservableCollection(Of String)
        DriveAdded = ""
        DriveRemoved = ""

        For Each i In FileIO.FileSystem.Drives
            If i.IsReady Then
                CollectionTemp.Add(i.Name & " [" & i.VolumeLabel & "]")
                CopieNewCollection.Add(i.Name & " [" & i.VolumeLabel & "]")
            Else
                CollectionTemp.Add(i.Name & " [ non disponible ]")
                CopieNewCollection.Add(i.Name & " [ non disponible ]")
            End If
        Next
        If CopieNewCollection.Count > DiskCollection.Count Then
            For Each i In DiskCollection
                If CopieNewCollection.Contains(i) Then CopieNewCollection.Remove(i)
            Next
            If CopieNewCollection.Count > 0 Then _
                DriveAdded = Trim(ExtraitChaine(CopieNewCollection.First, "", "["))
        Else
            For Each i In CopieNewCollection
                If DiskCollection.Contains(i) Then DiskCollection.Remove(i)
            Next
            If DiskCollection.Count > 0 Then _
                DriveRemoved = Trim(ExtraitChaine(DiskCollection.First, "", "["))
        End If
        DiskCollection = CollectionTemp
    End Sub

End Class

