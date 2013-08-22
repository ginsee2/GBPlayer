Option Compare Text
'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 04/01/12
'DESCRIPTION : Classe representant un disque CD
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.IO
Imports System.Xml
Imports System.Runtime.InteropServices

Public Class BassCDDisc
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************

    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Private DriveParent As BassCDDrive
    Private DiscTOC As BASS_CD_TOC
    Private Various As Boolean
    Shared CompteurRequete As Integer
    '-INFORMATIONS SUR L'ETAT DU LECTEUR DE CD---------------------------------------
    Public ReadOnly Property DiscDriveNum As Integer
        Get
            If DriveParent IsNot Nothing Then Return DriveParent.DriveNum Else Return Nothing
        End Get
    End Property
    Public ReadOnly Property DiscIsAudio As Boolean
        Get
            If DiscTOC.last > 0 Then Return (New DriveInfo(DriveParent.DriveName).VolumeLabel = "Audio CD") Else Return False
        End Get
    End Property
    Public ReadOnly Property DiscTracksCount() As Long
        Get
            If DriveParent IsNot Nothing Then Return BASS_CD_GetTracks(DriveParent.DriveNum) Else Return 0
        End Get
    End Property
    Public ReadOnly Property DiscTrackLenght(ByVal Track As Integer) As Long
        Get
            If Track > DiscTOC.last Then Return 0
            Dim Retour = 2352 * (DiscTOC.tracks(Track).lba - DiscTOC.tracks(Track - 1).lba)
            'Dim Retour = BASS_CD_GetTrackLength(DriveParent.DriveNum, Track - 1) - BASS_CD_GetTrackPregap(DriveParent.DriveNum, Track - 1)
            If Retour > 0 Then Return Retour Else Return 0
        End Get
    End Property
    Public ReadOnly Property DiscTrackDuration(ByVal Track As Integer) As Long
        Get
            If Track > DiscTOC.last Then Return 0
            Return DiscTrackLenght(Track) / (4 * 44100)
        End Get
    End Property
    Public ReadOnly Property DiscTracks As XDocument
        Get
            Dim RequeteQuery As String = Marshal.PtrToStringAnsi(BASS_CD_GetID(DriveParent.DriveNum, Enum_Bass_CD_GetID.BASS_CDID_CDDB)) ' + CompteurRequete))
            Dim IdDisc As String = ""
            If RequeteQuery <> "" Then IdDisc = ExtraitChaine(RequeteQuery, "", " ")
            Dim RequeteCDDB As String = Marshal.PtrToStringAnsi(BASS_CD_GetID(DriveParent.DriveNum, Enum_Bass_CD_GetID.BASS_CDID_CDDB_READ)) ' + CompteurRequete))
            Dim Compteur As Integer = 0
            If IdDisc <> GetCDDBInfo(RequeteCDDB, "DISCID") Then RequeteCDDB = Nothing
            Dim InfosXml As XDocument = _
                    <?xml version="1.0" encoding="utf-8" standalone="yes"?>
                    <CDDISK>
                        <Tracks>
                            <Id><%= GetCDDBInfo(RequeteCDDB, "DISCID") %></Id>
                            <Album><%= GetCDDBInfo(RequeteCDDB, "DTITLE", "Album", True) %></Album>
                            <Artiste><%= GetCDDBInfo(RequeteCDDB, "DTITLE", "Artiste") %></Artiste>
                            <Annee><%= GetCDDBInfo(RequeteCDDB, "DYEAR") %></Annee>
                            <%= From i In DiscTOC.tracks
                                Where i.adrcon > 0 And i.track < 100
                                Select CreationxmlTrack(i, Compteur, RequeteCDDB) %>
                        </Tracks>
                    </CDDISK>
            Return InfosXml
        End Get
    End Property
    '***********************************************************************************************
    '----------------------------------CONSTRUCTEUR DE LA CLASSE------------------------------------
    '***********************************************************************************************
    Public Sub New(ByVal Drive As BassCDDrive)
        If Drive IsNot Nothing Then
            DriveParent = Drive
            Dim retour = BASS_CD_GetTOC(Drive.DriveNum, 0, DiscTOC)
            '   CompteurRequete += 1
            '   If CompteurRequete > 10 Then CompteurRequete = 0
        End If
    End Sub

    '***********************************************************************************************
    '----------------------------------DESTRUCTION DE LA CLASSE-------------------------------------
    '***********************************************************************************************

    '***********************************************************************************************
    '----------------------------------PROCEDURE PUBLIQUES-------------------------------------
    '***********************************************************************************************

    '***********************************************************************************************
    '----------------------------------FONCTIONS UTILITAIRES-------------------------------------
    '***********************************************************************************************
    Private Function CreationxmlTrack(ByVal Element As BASS_CD.BASS_CD_TOC_TRACK, ByRef Compteur As Integer, ByVal InfosCDDB As String)
        If Various Then
            Dim InfoTrack As XElement = _
            <Track>
                <Numero><%= Compteur %></Numero>
                <Piste><%= Element.track.ToString("00") %></Piste>
                <PositionFrame><%= Element.lba %></PositionFrame>
                <Artiste><%= GetCDDBInfo(InfosCDDB, "TTITLE" & Compteur, "Artiste " & Compteur + 1) %></Artiste>
                <Album><%= GetCDDBInfo(InfosCDDB, "DTITLE", "Album", True) %></Album>
                <Titre><%= GetCDDBInfo(InfosCDDB, "TTITLE" & Compteur, "Titre " & Compteur + 1, True) %></Titre>
                <LongueurEnByte><%= Me.DiscTrackLenght(Element.track) %></LongueurEnByte>
                <Duree><%= Me.DiscTrackDuration(Element.track) %></Duree>
            </Track>
            Compteur += 1
            Return InfoTrack
        Else
            Dim InfoTrack As XElement = _
            <Track>
                <Numero><%= Compteur %></Numero>
                <Piste><%= Element.track.ToString("00") %></Piste>
                <PositionFrame><%= Element.lba %></PositionFrame>
                <Artiste><%= GetCDDBInfo(InfosCDDB, "DTITLE", "Artiste") %></Artiste>
                <Album><%= GetCDDBInfo(InfosCDDB, "DTITLE", "Album", True) %></Album>
                <Titre><%= GetCDDBInfo(InfosCDDB, "TTITLE" & Compteur, "Titre " & Compteur + 1) %></Titre>
                <LongueurEnByte><%= Me.DiscTrackLenght(Element.track) %></LongueurEnByte>
                <Duree><%= Me.DiscTrackDuration(Element.track) %></Duree>
            </Track>
            Compteur += 1
            Return InfoTrack
        End If
    End Function
    Private Function GetCDDBInfo(ByVal CDDBInfos As String, ByVal ChaineInfo As String,
                                        Optional ByVal InfoRemplacement As String = "",
                                        Optional ByVal FinDesignation As Boolean = False) As String
        Dim Lecture As String = ExtraitChaine(CDDBInfos, ChaineInfo & "=", Chr(13), ChaineInfo.Length + 1)
        If Lecture = "" Then Return InfoRemplacement
        Dim Debut As String = Trim(ExtraitChaine(Lecture, "", "/"))
        If ChaineInfo = "DTITLE" Then If InStr(Debut, "Various") > 0 Then Various = True Else Various = False
        If Debut <> "" Then
            If FinDesignation Then Return Trim(ExtraitChaine(Lecture, "/", "")) Else Return Debut
        End If
        Return Lecture
    End Function

End Class
