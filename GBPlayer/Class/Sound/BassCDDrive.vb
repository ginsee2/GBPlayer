'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 31/12/11
'DESCRIPTION : Classe representant un lecteur de CD
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.IO
Imports System.Management
Imports System.Runtime.InteropServices
Public Class BassCDDrive
    Implements IDisposable
    Public Enum EnumDriveUser
        Libre
        Player
        Converter
    End Enum
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Public Event DiscEjected()
    Public Event DiscInsered()
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Private Shared ListDriveUser As Dictionary(Of Integer, EnumDriveUser) = New Dictionary(Of Integer, EnumDriveUser)
    Private InfoCdDrive As DriveInfo
    Private InfoHardDrive As BASS_CD_INFO
    Private NumDrive As UInteger
    Private WithEvents ScanDisque As wpfDrives

    Public Shared Property DriveUser(ByVal num As Integer) As EnumDriveUser
        Get
            Try
                Return ListDriveUser.Item(num)
            Catch ex As Exception
                Return EnumDriveUser.Libre
            End Try
        End Get
        Set(ByVal user As EnumDriveUser)
            ListDriveUser.Item(num) = user
        End Set
    End Property
    Public Shared ReadOnly Property DriveIsBusy(ByVal num As Integer) As Boolean
        Get
            If DriveUser(num) <> EnumDriveUser.Libre Then Return True Else Return False
        End Get
    End Property

    '-INFORMATIONS SUR L'ETAT DU LECTEUR DE CD---------------------------------------
    Public ReadOnly Property DriveIsReady() As Boolean
        Get
            If InfoCdDrive IsNot Nothing Then Return InfoCdDrive.IsReady Else Return False
        End Get
    End Property
    Public Property DriveIsLock As Boolean
        Get
            Return BASS_CD_DoorIsLocked(NumDrive)
        End Get
        Set(ByVal value As Boolean)
            BASS_CD_Door(NumDrive, IIf(value, Enum_BassCD_Door.BASS_CD_DOOR_LOCK, Enum_BassCD_Door.BASS_CD_DOOR_UNLOCK))
        End Set
    End Property
    Public Property DriveIsOpen As Boolean
        Get
            Return BASS_CD_DoorIsOpen(NumDrive)
        End Get
        Set(ByVal value As Boolean)
            BASS_CD_Door(NumDrive, IIf(value, Enum_BassCD_Door.BASS_CD_DOOR_OPEN, Enum_BassCD_Door.BASS_CD_DOOR_CLOSE))
        End Set
    End Property
    Public Property DriveSpeed As UInt32
        Get
            Return BASS_CD_GetSpeed(NumDrive)
        End Get
        Set(ByVal value As UInt32)
            BASS_CD_SetSpeed(NumDrive, value)
        End Set
    End Property
    '-INFORMATIONS SUR LE LECTEUR DE CD---------------------------------------
    Public ReadOnly Property DriveName() As String
        Get
            If InfoCdDrive IsNot Nothing Then Return InfoCdDrive.Name Else Return ""
        End Get
    End Property
    Public ReadOnly Property DriveNum() As UInt32
        Get
            Return NumDrive
        End Get
    End Property
    Public ReadOnly Property DriveVolumeLabel() As String
        Get
            If InfoCdDrive IsNot Nothing Then Return InfoCdDrive.VolumeLabel Else Return ""
        End Get
    End Property
    Public ReadOnly Property DriveVendor As String
        Get
            Return Marshal.PtrToStringAnsi(InfoHardDrive.vendor)
        End Get
    End Property
    Public ReadOnly Property DriveModel As String
        Get
            Return Marshal.PtrToStringAnsi(InfoHardDrive.product)
        End Get
    End Property
    Public ReadOnly Property DriveVersion As String
        Get
            Return Marshal.PtrToStringAnsi(InfoHardDrive.rev)
        End Get
    End Property
    Public ReadOnly Property DriveCapabilities(ByVal Flag As Enum_Bass_CD_rwflag) As Boolean
        Get
            Return (InfoHardDrive.rwflags And Flag) = Flag
        End Get
    End Property

    '***********************************************************************************************
    '----------------------------------CONSTRUCTEUR DE LA CLASSE------------------------------------
    '***********************************************************************************************
    Public Sub New(ByVal DriveName As String)
        Dim NumberCD As Integer
        For Each i In FileIO.FileSystem.Drives
            If (ExtraitChaine(i.Name, "", ":") = ExtraitChaine(DriveName, "", ":")) And i.DriveType = DriveType.CDRom Then
                NumDrive = NumberCD
                InfoCdDrive = New DriveInfo(DriveName)
                InfoHardDrive = New BASS_CD_INFO
                BASS_CD_GetInfo(NumDrive, InfoHardDrive)
                ScanDisque = New wpfDrives(wpfDrives.EnumDriveUpdate.CDRomModification)
                Exit For
            End If
            If i.DriveType = DriveType.CDRom Then NumberCD += 1
        Next
    End Sub

    '***********************************************************************************************
    '----------------------------------DESTRUCTION DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Private disposedValue As Boolean ' Pour détecter les appels redondants
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                If ScanDisque IsNot Nothing Then ScanDisque.Dispose()
            End If
        End If
        Me.disposedValue = True
    End Sub

    '***********************************************************************************************
    '----------------------------------PROCEDURE PUBLIQUES-------------------------------------
    '***********************************************************************************************
    Public Function GetDisc() As BassCDDisc
        If DriveIsReady Then Return New BassCDDisc(Me)
    End Function
    '***********************************************************************************************
    '----------------------------------FONCTIONS UTILITAIRES-------------------------------------
    '***********************************************************************************************
    Private Sub ScanDisque_DiskCDRomEjected(ByVal Name As String) Handles ScanDisque.DiskCDRomEjected
        If Name = DriveName Then If InfoCdDrive IsNot Nothing Then InfoCdDrive = New DriveInfo(InfoCdDrive.Name)
        RaiseEvent DiscEjected()
    End Sub
    Private Sub ScanDisque_DiskCDRomInsered(ByVal Name As String) Handles ScanDisque.DiskCDRomInsered
        If Name = DriveName Then If InfoCdDrive IsNot Nothing Then InfoCdDrive = New DriveInfo(InfoCdDrive.Name)
        RaiseEvent DiscInsered()
    End Sub

End Class
