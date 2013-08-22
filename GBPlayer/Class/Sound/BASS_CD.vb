Imports System.Runtime.InteropServices
Imports System.Text

'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 02/01/12
'DESCRIPTION :Module d'importation des fonction librairie BASSCD

Module BASS_CD
    '***********************************************************************************************
    '-----------------------------ENUMERATIONS UTILISEES--------------------------------------------
    '***********************************************************************************************
    Public Enum Enum_BassCD_Door
        BASS_CD_DOOR_CLOSE = 0
        BASS_CD_DOOR_OPEN = 1
        BASS_CD_DOOR_LOCK = 2
        BASS_CD_DOOR_UNLOCK = 3
    End Enum
    Public Enum Enum_Bass_CD_Toc_Track
        BASS_CD_TOC_CON_PRE = 1
        BASS_CD_TOC_CON_COPY = 2
        BASS_CD_TOC_CON_DATA = 4
    End Enum
    Public Enum Enum_Bass_CD_GetID
        BASS_CDID_UPC = 1
        BASS_CDID_CDDB = 2
        BASS_CDID_CDDB2 = 3
        BASS_CDID_TEXT = 4
        BASS_CDID_CDPLAYER = 5
        BASS_CDID_MUSICBRAINZ = 6
        BASS_CDID_ISRC = &H100 ' + track #
        BASS_CDID_CDDB_QUERY = &H200
        BASS_CDID_CDDB_READ = &H201 ' + entry #
        BASS_CDID_CDDB_READ_CACHE = &H2FF
    End Enum


    '***********************************************************************************************
    '-------------------------------STRUCTURES PUBLIQUES--------------------------------------------
    '***********************************************************************************************
    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi)> _
    Public Structure BASS_CD_INFO
        Public vendor As IntPtr       ' manufacturer
        Public product As IntPtr       ' model
        Public rev As IntPtr           ' revision
        Public letter As Int32        ' drive letter
        Public rwflags As Enum_Bass_CD_rwflag       ' read/write capability flags
        Public canopen As Int32       ' BASS_CD_DOOR_OPEN/CLOSE is supported?
        Public canlock As Int32       ' BASS_CD_DOOR_LOCK/UNLOCK is supported?
        Public maxspeed As UInt32      ' max read speed (KB/s)
        Public cache As UInt32         ' cache size (KB)
        Public cdtext As Int32        ' can read CD-TEXT
    End Structure
    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi)> _
    Public Structure BASS_CD_TOC
        Public size As UInt16       ' size of TOC
        Public first As Byte         ' first track
        Public last As Byte          ' last track
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=100)> _
        Public tracks As BASS_CD_TOC_TRACK() ' up to 100 tracks
    End Structure
    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi)> _
    Public Structure BASS_CD_TOC_TRACK
        Public res1 As Byte
        Public adrcon As Byte        ' ADR + control
        Public track As Byte         ' track number
        Public res2 As Byte
        Public lba As UInt32           ' start address (logical block address)
    End Structure

    '***********************************************************************************************
    '-------------------------------FONCTIONS IMPORTEES---------------------------------------------
    '***********************************************************************************************
    '---Importation de la librairie BASSCD
    <DllImport("Basscd.dll")> _
    Public Function BASS_CD_GetInfo(ByVal drive As UInt32, ByRef info As BASS_CD_INFO) As Int32
    End Function
    <DllImport("Basscd.dll")> _
    Public Function BASS_CD_Door(ByVal drive As UInt32, ByVal action As Enum_BassCD_Door) As Int32
    End Function
    <DllImport("Basscd.dll")> _
    Public Function BASS_CD_DoorIsLocked(ByVal drive As UInt32) As Int32
    End Function
    <DllImport("Basscd.dll")> _
    Public Function BASS_CD_DoorIsOpen(ByVal drive As UInt32) As Int32
    End Function
    <DllImport("Basscd.dll")> _
    Public Function BASS_CD_GetSpeed(ByVal drive As UInt32) As Int32
    End Function
    <DllImport("Basscd.dll")> _
    Public Function BASS_CD_SetSpeed(ByVal drive As UInt32, ByVal speed As Int32) As UInt32
    End Function
    <DllImport("Basscd.dll")> _
    Public Function BASS_CD_GetTOC(ByVal drive As UInt32, ByVal mode As UInt32, ByRef toc As BASS_CD_TOC) As Int32
    End Function
    <DllImport("Basscd.dll")> _
    Public Function BASS_CD_GetTracks(ByVal drive As UInt32) As UInt32
    End Function
    <DllImport("Basscd.dll")> _
    Public Function BASS_CD_GetTrackLength(ByVal drive As UInt32, ByVal track As UInt32) As UInt32
    End Function
    <DllImport("Basscd.dll")> _
    Public Function BASS_CD_GetTrackPregap(ByVal drive As UInt32, ByVal track As UInt32) As UInt32
    End Function
    <DllImport("Basscd.dll", CharSet:=CharSet.Ansi)> _
    Public Function BASS_CD_GetID(ByVal drive As UInt32, ByVal id As Enum_Bass_CD_GetID) As IntPtr
    End Function
    <DllImport("Basscd.dll", CharSet:=CharSet.Ansi)> _
    Public Function BASS_CD_StreamCreate(ByVal drive As UInt32, ByVal track As UInt32, ByVal flags As Enum_Bass_StreamCreate) As IntPtr
    End Function


    '---FONCTION UTILITAIRES PERMETTANT DE TESTER LE CODE DE RETOUR
    '---FONCTION UTILITAIRES PERMETTANT DE TESTER LE CODE DE RETOUR
    Private Enum Enum_Bass_CD_Error
        BASS_ERROR_NOCD = 12      ' no CD in drive
        BASS_ERROR_CDTRACK = 13   ' invalid track number
        BASS_ERROR_NOTAUDIO = 17  ' not an audio track
    End Enum
    Public Function BASS_CD_ErrorGetMessage(ByVal FonctionOrigine As String) As String
        Dim msg As String
        Select Case BASS_ErrorGetCode()
            Case Enum_Bass_CD_Error.BASS_ERROR_NOCD
                msg = "no CD in drive"
            Case Enum_Bass_CD_Error.BASS_ERROR_CDTRACK
                msg = "invalid track number"
            Case Enum_Bass_CD_Error.BASS_ERROR_NOTAUDIO
                msg = "not an audio track"
            Case Else
                msg = BASS_ErrorGetMessage(FonctionOrigine)
        End Select
        Return msg & " - " & FonctionOrigine
    End Function

End Module
