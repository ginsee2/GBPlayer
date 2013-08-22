Option Compare Text

Imports System.IO
Imports Newtonsoft.Json.Bson
Imports Newtonsoft.Json.Linq
Imports Newtonsoft.Json
Imports System.Net
Imports System.Reflection
Imports System.Xml
Imports System.Threading
Imports System.Windows.Threading

'***********************************************************************************************
'----------------------------CLASSE PERMET DE GERER LES INFORMATIONS VENANT DE DISCOGS----------
'***********************************************************************************************
Public Class Discogs
    Private Const GBAU_NOMDOSSIER_STOCKAGEDATA = "GBDev\GBPlayer\Data\Discogs"
    Private Const GBAU_NOMDOSSIER_STOCKAGEIMAGES = "GBDev\GBPlayer\Images\Discogs"
    Private Const GBAU_NOMRESSOURCE = "gbDev.DataModelDiscogsError.xml"
    Private Const Site = "api.discogs.com"

     Public Shared Function Get_ReleaseId(ByVal url As String) As String
        Dim Chaine As String
        If url <> "*" And url <> "" Then Chaine = ExtraitChaine(url, "/release", "", 9) Else Chaine = ""
        If InStr(Chaine, "?") > 0 Then Chaine = ExtraitChaine(Chaine, "", "?")
        Return Chaine
    End Function

    Public Sub New()
    End Sub
    Public Sub New(ByVal id As String)
        releaseObject = release(id)
    End Sub

    Private idEnCours As String
    Private releaseObject As DiscogsRelease
    Public ReadOnly Property release() As DiscogsRelease
        Get
            Return release(idEnCours)
        End Get
    End Property
    Public ReadOnly Property release(ByVal idRelease As String) As DiscogsRelease
        Get
            If idRelease <> idEnCours Then releaseObject = GetRelease(idRelease)
            If releaseObject Is Nothing Then releaseObject = GetRelease(idRelease)
            If releaseObject Is Nothing Then releaseObject = GetRelease(idRelease, True)
            If releaseObject Is Nothing Then releaseObject = New DiscogsRelease
            Return releaseObject
        End Get
    End Property
    Public Function getXmlRelease(ByVal idRelease As String, ByVal ChargementXml As Boolean) As String
        If idRelease = "" Then MemorisationErreurDiscogs(New Exception("id vide, requete impossible"), idRelease, "Release")
        If idRelease <> idEnCours Then releaseObject = GetRelease(idRelease, ChargementXml)
        If releaseObject Is Nothing Then releaseObject = GetRelease(idRelease)
        If releaseObject Is Nothing Then releaseObject = GetRelease(idRelease, True)
        If releaseObject Is Nothing Then releaseObject = New DiscogsRelease
        Return GetNomFichierXML(idRelease, "release", False)
    End Function

    Private idArtEnCours As String
    Private artisteObject As DiscogsArtiste
    Public ReadOnly Property artiste() As DiscogsArtiste
        Get
            Return artiste(idArtEnCours)
        End Get
    End Property
    Public ReadOnly Property artiste(ByVal idArtiste As String) As DiscogsArtiste
        Get
            If idArtiste <> idArtEnCours Then artisteObject = GetArtiste(idArtiste)
            If artisteObject Is Nothing Then artisteObject = GetArtiste(idArtiste)
            If artisteObject Is Nothing Then artisteObject = New DiscogsArtiste(Me)
            Return artisteObject
        End Get
    End Property

    Private idMasterEnCours As String
    Private masterObject As DiscogsMaster
    Private idMasterReleaseEnCours As String
    Public ReadOnly Property master() As DiscogsMaster
        Get
            Return master(idMasterEnCours)
        End Get
    End Property
    Public ReadOnly Property master(ByVal idMaster As String) As DiscogsMaster
        Get
            If idMaster <> idMasterEnCours Then masterObject = GetMaster(idMaster)
            If masterObject Is Nothing Then masterObject = GetMaster(idMasterEnCours)
            If masterObject Is Nothing Then masterObject = New DiscogsMaster(Me)
            Return masterObject
        End Get
    End Property
    Public Function getXmlMaster(ByVal idMaster As String) As String
        If idMaster = "" Then MemorisationErreurDiscogs(New Exception("id vide, requete impossible"), idMaster, "Master")
        If idMaster <> idMasterEnCours Then masterObject = GetMaster(idMaster)
        If masterObject Is Nothing Then masterObject = GetMaster(idMasterEnCours)
        If masterObject Is Nothing Then masterObject = New DiscogsMaster(Me)
        Return GetNomFichierXML(idMaster, "master", False)
    End Function
    Public Function getXmlMasterReleases(ByVal idMaster As String, ByRef Page As Integer) As String
        Dim FichierxmlMaster As String
        If idMasterEnCours <> idMaster Then FichierxmlMaster = getXmlMaster(idMaster)
        GetMasterReleases(master, Page)
        Return GetNomFichierXML(idMaster, "masterreleases", False)
    End Function

    Private idLabelEnCours As String
    Private labelObject As DiscogsLabel
    Public ReadOnly Property label() As DiscogsLabel
        Get
            Return label(idLabelEnCours)
        End Get
    End Property
    Public ReadOnly Property label(ByVal idLabel As String) As DiscogsLabel
        Get
            If idLabel <> idLabelEnCours Then labelObject = GetLabel(idLabel)
            If labelObject Is Nothing Then labelObject = GetLabel(idLabel)
            If labelObject Is Nothing Then labelObject = New DiscogsLabel(Me)
            Return labelObject
        End Get
    End Property

    Private Function GetRelease(ByVal idRelease As String, Optional ByVal ChargementXml As Boolean = False) As DiscogsRelease
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim PathNomFichierInfos As String = GetNomFichierXML(idRelease, "release")
        If idRelease <> "" Then
            If idRelease <> idEnCours Then
                idEnCours = ""
                Dim nid As DiscogsRelease = New DiscogsRelease
                Try
                    Dim resp As XDocument
                    If Not ChargementXml Then
                        hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/releases/" & idRelease)
                        reader = New IO.StreamReader(hwebresponse.GetResponseStream)
                        resp = ConvertReponseXml(reader)
                    Else
                        resp = XDocument.Load(PathNomFichierInfos)
                    End If
                    resp.Save(PathNomFichierInfos)
                    nid.id = idRelease
                    nid.artiste.anv = (From art In resp...<discogs>.<artists>
                                Select art.<anv>.Value).First
                    nid.artiste.nom = (From art In resp...<discogs>.<artists>
                                Select art.<name>.Value).First
                    nid.artiste.id = (From art In resp...<discogs>.<artists>
                                Select art.<id>.Value).First
                    nid.titre = (From Tit In resp...<discogs>.<title>
                                    Select Tit.Value).First
                    Dim newLabel As New DiscogsNomLabel((From lab In resp...<discogs>.<labels>
                            Select lab.<name>.Value).First)
                    newLabel.catalogue = (From lab In resp...<discogs>.<labels>
                                Select lab.<catno>.Value).First
                    newLabel.id = (From lab In resp...<discogs>.<labels>
                                Select lab.<id>.Value).First
                    nid.label = newLabel
                    Dim newAnnee = (From Ann In resp...<discogs>.<year>
                                    Select Ann.Value)
                    If newAnnee.Count > 0 Then nid.annee = newAnnee.First
                    Dim newformat = (From F In resp...<discogs>.<formats>
                            Select F.<descriptions>.Value)
                    If newformat.Count > 0 Then nid.format = newformat.First
                    Dim newPays = (From Ann In resp...<discogs>.<country>
                           Select Ann.Value)
                    If newPays.Count > 0 Then nid.pays = newPays.First
                    Dim newnote = (From note In resp...<discogs>.<notes>
                                    Select note.Value)
                    If newnote.Count > 0 Then nid.notes = newnote.First
                    Dim ListeStyle = (From Sty In resp...<discogs>.<styles>
                            Select Sty.Value)
                    If ListeStyle.Count > 0 Then nid.style = ListeStyle.First
                    Dim ListeGenre = (From Sty In resp...<discogs>.<genres>
                            Select Sty.Value)
                    If ListeGenre.Count > 0 Then nid.genre = ListeGenre.First
                    Dim newidmaster = (From idm In resp...<discogs>.<master_id>
                            Select idm.Value)
                    If newidmaster.Count > 0 Then nid.idMaster = newidmaster.First
                    Dim RequeteListeImages = (From lab In resp...<discogs>.<images>
                                         Select lab)
                    For Each i In RequeteListeImages
                        Dim newDiscogsImage As New DiscogsImage
                        newDiscogsImage.urlImage = i.<uri>.Value
                        newDiscogsImage.hauteur = i.<height>.Value
                        newDiscogsImage.largeur = i.<width>.Value
                        newDiscogsImage.urlImage150 = i.<uri150>.Value
                        ' newDiscogsImage.typeImage = i.<type>.Value
                        If i.<type>.Value = "primary" Then
                            nid.images.Insert(0, newDiscogsImage)
                        Else
                            nid.images.Add(newDiscogsImage)
                        End If
                    Next
                    Dim ListeWritter = From Tit In resp...<discogs>.<extraartists>
                                                      Where Tit.<role>.Value Like "*Written*" Or
                                                            Tit.<role>.Value Like "*Composed*" Or
                                                            Tit.<role>.Value Like "*Lyrics By*" Or
                                                            Tit.<role>.Value Like "*Words By*" Or
                                                            Tit.<role>.Value Like "*SongWriter*" Or
                                                            Tit.<role>.Value Like "*Music By*"
                                                      Select Tit.<name>.Value, Tit.<tracks>.Value


                    Dim ListeTrackWritter = From Tit In resp...<discogs>.<tracklist>.<extraartists>
                                                            Where (Tit.<role>.Value Like "*Written*") Or
                                                                  Tit.<role>.Value Like "*Composed*" Or
                                                                  Tit.<role>.Value Like "*Lyrics By*" Or
                                                                  Tit.<role>.Value Like "*Words By*" Or
                                                                  Tit.<role>.Value Like "*SongWriter*" Or
                                                                 Tit.<role>.Value Like "*Music By*"
                                                         Select Tit.Ancestors.<position>.Value, Tit.<name>.Value

                    Dim ListeTitresT = From Tit In resp...<discogs>.<tracklist>
                                      Select Tit.<position>.Value, Tit.<title>.Value, Tit.<duration>.Value

                    Dim TabComp As New List(Of String)
                    For Each i In ListeWritter
                        Dim Groupes() As String = Split(i.tracks, ",")
                        Dim Compositeur As String = i.name
                        If Groupes.Count > 1 Then
                            Array.ForEach(Groupes, Sub(c As String)
                                                       Dim sousGroupes = Split(c, "to")
                                                       'If Not (sousGroupes.Count > 1) Then
                                                       '    sousGroupes = Split(c, "-")
                                                       'End If
                                                       If sousGroupes.Count > 1 Then
                                                           Dim NomPiste As String = Trim(sousGroupes(0))
                                                           Dim PosTitre As Integer
                                                           For Each j In ListeTitresT
                                                               If j.position = NomPiste Then Exit For
                                                               PosTitre += 1
                                                           Next
                                                           Do
                                                               NomPiste = ListeTitresT.ElementAt(PosTitre).position
                                                               PosTitre += 1
                                                               TabComp.Add("<" & NomPiste & "> " & Compositeur)
                                                               If NomPiste = Trim(sousGroupes(1)) Then Exit Do
                                                           Loop Until PosTitre > ListeTitresT.Count
                                                       Else
                                                           TabComp.Add("<" & Trim(c) & "> " & Compositeur)
                                                       End If
                                                   End Sub)
                        Else
                            TabComp.Add("<" & i.tracks & "> " & i.name)
                        End If
                    Next

                    For Each i In ListeTrackWritter
                        TabComp.Add("<" & i.position & "> " & i.name)
                    Next
                    For Each i In ListeTitresT
                        Dim newPiste As New DiscogsPiste
                        If i.position <> "" Then
                            newPiste.numPiste = i.position
                        End If
                        newPiste.nomPiste = i.title
                        newPiste.dureePiste = i.duration
                        nid.pistes.Add(newPiste)

                        Dim newCompositeur As New DiscogsCompositeur
                        newCompositeur.numPiste = newPiste.numPiste ' i.position
                        newCompositeur.nomPiste = i.title
                        TabComp.ForEach(Sub(c As String)
                                            If (ExtraitChaine(c, "<", ">") = "" Or ExtraitChaine(c, "<", ">") = newCompositeur.numPiste) And
                                                InStr(newCompositeur.nomCompositeurs, Trim(ExtraitChaine(c, ">", ""))) = 0 Then
                                                If newCompositeur.nomCompositeurs <> "" Then newCompositeur.nomCompositeurs += ", "
                                                newCompositeur.nomCompositeurs += Trim(ExtraitChaine(c, ">", ""))
                                            End If
                                        End Sub)
                        If Right(newCompositeur.nomCompositeurs, 1) = "," Then newCompositeur.nomCompositeurs = Left(newCompositeur.nomCompositeurs, newCompositeur.nomCompositeurs.Length - 1)
                        nid.compositeurs.Add(newCompositeur)
                    Next
                    idEnCours = nid.id
                    releaseObject = nid
                    Return releaseObject
                Catch ex As Exception
                    '    wpfMsgBox.MsgBoxInfo("Erreur DISCOGS", ex.Message, Nothing, "Erreur sur release " & idRelease)
                    MemorisationErreurDiscogs(ex, idRelease, "Release")
                    Return Nothing
                Finally
                    If reader IsNot Nothing Then reader.Close()
                    If hwebresponse IsNot Nothing Then hwebresponse.Close()
                End Try
            End If
        Else
            releaseObject = Nothing
            Return Nothing
        End If
    End Function
    Private Function GetArtiste(ByVal idArtiste As String) As DiscogsArtiste
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        If idArtiste <> "" Then
            If idArtiste <> idArtEnCours Then
                Dim nartiste As DiscogsArtiste = New DiscogsArtiste(Me)
                idArtEnCours = idArtiste
                Try
                    Dim resp As XDocument
                    hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/artists/" & idArtiste)
                    reader = New IO.StreamReader(hwebresponse.GetResponseStream)
                    resp = ConvertReponseXml(reader)
                    nartiste.id = idArtiste
                    nartiste.nom = (From art In resp...<discogs>.<name>
                                Select art.Value).First
                    Dim r = (From val In resp...<discogs>.<realname>
                                Select val.Value)
                    If r.Count > 0 Then nartiste.nomReel = r.First
                    r = From Prof In resp...<discogs>.<profile>
                                    Select Prof.Value
                    If r.Count > 0 Then nartiste.profile = r.First
                    Dim RequeteVariationsNoms = (From nv In resp...<discogs>.<namevariations>
                                        Select nv)
                    If RequeteVariationsNoms IsNot Nothing Then
                        For Each i In RequeteVariationsNoms
                            nartiste.autresNoms.Add(i.Value)
                        Next
                    End If
                    Dim Requeteurls = (From url In resp...<discogs>.<urls>
                                        Select url)
                    If Requeteurls IsNot Nothing Then
                        For Each i In Requeteurls
                            nartiste.urls.Add(i.Value)
                        Next
                    End If
                    Dim RequeteMembres = (From mem In resp...<discogs>.<members>
                                        Select mem)
                    For Each i In RequeteMembres
                        Dim newMembre As New DiscogsNomArtiste
                        newMembre.nom = i.<name>.Value
                        newMembre.id = i.<id>.Value
                        nartiste.membres.Add(newMembre)
                    Next
                    Dim Requetealiases = (From mem In resp...<discogs>.<aliases>
                                        Select mem)
                    For Each i In Requetealiases
                        Dim newalias As New DiscogsNomArtiste
                        newalias.nom = i.<name>.Value
                        newalias.id = i.<id>.Value
                        nartiste.aliases.Add(newalias)
                    Next
                    Dim RequeteListeImages = (From lab In resp...<discogs>.<images>
                                        Select lab)
                    nartiste.images.Clear()
                    For Each i In RequeteListeImages
                        Dim newDiscogsImage As New DiscogsImage
                        newDiscogsImage.urlImage = i.<uri>.Value
                        newDiscogsImage.hauteur = i.<height>.Value
                        newDiscogsImage.largeur = i.<width>.Value
                        newDiscogsImage.urlImage150 = i.<uri150>.Value
                        If i.<type>.Value = "primary" Then
                            nartiste.images.Insert(0, newDiscogsImage)
                        Else
                            nartiste.images.Add(newDiscogsImage)
                        End If
                    Next
                    idArtEnCours = nartiste.id
                    artisteObject = nartiste
                    Return artisteObject
                Catch ex As Exception
                    ' wpfMsgBox.MsgBoxInfo("Erreur DISCOGS", ex.Message, Nothing, "Erreur sur artiste :" & idArtiste)
                    MemorisationErreurDiscogs(ex, idArtiste, "Artiste")
                    Return Nothing
                Finally
                    If reader IsNot Nothing Then reader.Close()
                    If hwebresponse IsNot Nothing Then hwebresponse.Close()
                End Try
            End If
        Else
            releaseObject = Nothing
            Return Nothing
        End If
    End Function
    Public Function GetArtisteReleases(ByVal idArtiste As DiscogsArtiste, ByVal NumPage As Integer) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        If idArtiste IsNot Nothing Then
            Try
                Dim resp As XDocument
                hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/artists/" & idArtiste.id & "/releases", "page=" & NumPage.ToString & "&per_page=100")
                reader = New IO.StreamReader(hwebresponse.GetResponseStream)
                resp = ConvertReponseXml(reader)
                idArtiste.nbrReleases = CInt((From rel In resp...<discogs>.<pagination>.<items>
                                       Select rel.Value).First)
                Dim RequeteReleases = (From rel In resp...<discogs>.<releases>
                                    Select rel)
                For Each i In RequeteReleases
                    Dim newDiscogsReleases As New DiscogsNomRelease
                    newDiscogsReleases.id = i.<id>.Value
                    newDiscogsReleases.titre = i.<title>.Value
                    newDiscogsReleases.format = i.<format>.Value
                    newDiscogsReleases.label = New DiscogsNomLabel(i.<label>.Value)
                    newDiscogsReleases.annee = i.<year>.Value
                    newDiscogsReleases.thumb = i.<thumb>.Value
                    newDiscogsReleases.type = i.<type>.Value
                    If newDiscogsReleases.type = "master" Then newDiscogsReleases.idMainRelease = i.<main_release>.Value
                    idArtiste.releases.Add(newDiscogsReleases)
                Next
                Return True
            Catch ex As Exception
                '  wpfMsgBox.MsgBoxInfo("Erreur DISCOGS", ex.Message, Nothing, "Erreur sur release artiste :" & idArtiste.id)
                MemorisationErreurDiscogs(ex, idArtiste.id, "ArtisteReleases")
                Return False
            Finally
                If reader IsNot Nothing Then reader.Close()
                If hwebresponse IsNot Nothing Then hwebresponse.Close()
            End Try
        Else
            Return False
        End If
    End Function
    Private Function GetMaster(ByVal idMaster As String) As DiscogsMaster
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim PathNomFichierInfos As String = GetNomFichierXML(idMaster, "master")
        If idMaster <> "" Then
            If idMaster <> idMasterEnCours Then
                idMasterEnCours = ""
                Dim nid As DiscogsMaster = New DiscogsMaster(Me)
                Try
                    Dim resp As XDocument
                    hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/masters/" & idMaster)
                    reader = New IO.StreamReader(hwebresponse.GetResponseStream)
                    resp = ConvertReponseXml(reader)
                    resp.Save(PathNomFichierInfos)
                    nid.id = idMaster
                    nid.artiste.anv = (From art In resp...<discogs>.<artists>
                                Select art.<anv>.Value).First
                    nid.artiste.nom = (From art In resp...<discogs>.<artists>
                                Select art.<name>.Value).First
                    nid.artiste.id = (From art In resp...<discogs>.<artists>
                                Select art.<id>.Value).First
                    nid.titre = (From Tit In resp...<discogs>.<title>
                                    Select Tit.Value).First
                    Dim newAnnee = (From Ann In resp...<discogs>.<year>
                                    Select Ann.Value)
                    If newAnnee.Count > 0 Then nid.annee = newAnnee.First
                    Dim newidMain = (From F In resp...<discogs>.<main_release>
                            Select F.Value)
                    If newidMain.Count > 0 Then nid.idMainRelease = newidMain.First
                    Dim newformat = (From F In resp...<discogs>.<formats>
                            Select F.<descriptions>.Value)
                    If newformat.Count > 0 Then nid.format = newformat.First
                    Dim ListeStyle = (From Sty In resp...<discogs>.<styles>
                            Select Sty.Value)
                    If ListeStyle.Count > 0 Then nid.style = ListeStyle.First
                    Dim ListeGenre = (From Sty In resp...<discogs>.<genres>
                            Select Sty.Value)
                    If ListeGenre.Count > 0 Then nid.genre = ListeGenre.First
                    Dim RequeteListeImages = (From lab In resp...<discogs>.<images>
                                         Select lab)
                    For Each i In RequeteListeImages
                        Dim newDiscogsImage As New DiscogsImage
                        newDiscogsImage.urlImage = i.<uri>.Value
                        newDiscogsImage.hauteur = i.<height>.Value
                        newDiscogsImage.largeur = i.<width>.Value
                        newDiscogsImage.urlImage150 = i.<uri150>.Value
                        If i.<type>.Value = "primary" Then
                            nid.images.Insert(0, newDiscogsImage)
                        Else
                            nid.images.Add(newDiscogsImage)
                        End If
                    Next
                    Dim RequeteListe = (From lab In resp...<discogs>.<tracklist>
                                          Select lab)
                    For Each i In RequeteListe
                        Dim newtracklist As New DiscogsPiste
                        newtracklist.dureePiste = i.<duration>.Value
                        newtracklist.numPiste = i.<position>.Value
                        newtracklist.nomPiste = i.<title>.Value
                        nid.pistes.Add(newtracklist)
                    Next

                    idMasterEnCours = nid.id
                    masterObject = nid
                    Return masterObject
                Catch ex As Exception
                    '   wpfMsgBox.MsgBoxInfo("Erreur DISCOGS", ex.Message, Nothing, "Erreur sur master " & idMaster)
                    MemorisationErreurDiscogs(ex, idMaster, "Master")
                    Return Nothing
                Finally
                    If reader IsNot Nothing Then reader.Close()
                    If hwebresponse IsNot Nothing Then hwebresponse.Close()
                End Try
            End If
        Else
            releaseObject = Nothing
            Return Nothing
        End If
    End Function
    Public Function GetMasterReleases(ByVal idMaster As DiscogsMaster, ByVal NumPage As Integer) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim PathNomFichierInfos As String = GetNomFichierXML(idMaster.id, "masterreleases")
        If idMaster IsNot Nothing Then
            If idMaster.id <> "" Then
                If idMaster.id <> idMasterReleaseEnCours Then
                    idMasterReleaseEnCours = ""
                    Dim nid As DiscogsMaster = New DiscogsMaster(Me)
                    Try
                        Dim resp As XDocument
                        hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/masters/" & idMaster.id & "/versions", "page=" & NumPage.ToString & "&per_page=100")
                        reader = New IO.StreamReader(hwebresponse.GetResponseStream)
                        resp = ConvertReponseXml(reader)
                        Dim NouveauFichier As XDocument = _
                                 <?xml version="1.0" encoding="utf-8"?>
                                 <resp>
                                     <discogs>
                                         <%= From i In resp.<resp>.<discogs>.<versions>
                                             Order By i.<released>.Value Ascending, i.<format>.Value Ascending
                                             Select i %>
                                     </discogs>
                                 </resp>
                        NouveauFichier.Save(PathNomFichierInfos)
                        ' resp.Save(PathNomFichierInfos)
                        idMaster.nbrVersions = CInt((From rel In resp...<discogs>.<pagination>.<items>
                                               Select rel.Value).First)
                        Dim RequeteReleases = (From rel In resp...<discogs>.<versions>
                                            Select rel)
                        For Each i In RequeteReleases
                            Dim newDiscogsReleases As New DiscogsNomRelease
                            newDiscogsReleases.id = i.<id>.Value
                            newDiscogsReleases.titre = i.<title>.Value
                            newDiscogsReleases.thumb = i.<thumb>.Value
                            newDiscogsReleases.pays = i.<country>.Value
                            newDiscogsReleases.format = i.<format>.Value
                            newDiscogsReleases.label = New DiscogsNomLabel(i.<label>.Value)
                            newDiscogsReleases.label.catalogue = i.<catno>.Value
                            newDiscogsReleases.annee = ExtraitChaine(i.<released>.Value, "", "-", , True)
                            idMaster.versions.Add(newDiscogsReleases)
                        Next
                        idMasterReleaseEnCours = idMaster.id
                        Return True
                    Catch ex As Exception
                        wpfMsgBox.MsgBoxInfo("Erreur DISCOGS", ex.Message, Nothing, "Erreur sur releases master :" & idMaster.id)
                        MemorisationErreurDiscogs(ex, idMaster.id, "MasterRelease")
                        Return False
                    Finally
                        If reader IsNot Nothing Then reader.Close()
                        If hwebresponse IsNot Nothing Then hwebresponse.Close()
                    End Try
                End If
            End If
        Else
            Return False
        End If
    End Function
    Private Function GetLabel(ByVal idLabel As String) As DiscogsLabel
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        If idLabel <> "" Then
            If idLabel <> idLabelEnCours Then
                Dim newlabel As DiscogsLabel = New DiscogsLabel(Me)
                idLabelEnCours = idLabel
                Try
                    Dim resp As XDocument
                    'hwebresponse = oAuthDiscogs.GetWebRequestoAuth("http://" & Site & "/releases/" & ReleaseNumber) ' hwebrequest.GetResponse
                    'Dim request As HttpWebRequest = System.Net.WebRequest.Create("http://" & Site & "/labels/" & idLabel)
                    'request.UserAgent = "GBPLayer3.0 2011/Application Discogs API v2"
                    'request.Headers.Add("Accept-Encoding", "gzip")
                    'request.AutomaticDecompression = Net.DecompressionMethods.GZip
                    'hwebresponse = DirectCast(request.GetResponse, HttpWebResponse)
                    hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/labels/" & idLabel)
                    reader = New IO.StreamReader(hwebresponse.GetResponseStream)
                    resp = ConvertReponseXml(reader)
                    newlabel.id = idLabel
                    newlabel.nom = (From art In resp...<discogs>.<name>
                                Select art.Value).First
                    Dim r = From Prof In resp...<discogs>.<profile>
                                    Select Prof.Value
                    If r.Count > 0 Then newlabel.profile = r.First
                    Dim newcontact = (From contact In resp...<discogs>.<contact_info>
                                    Select contact.Value)
                    If newcontact.Count > 0 Then newlabel.infoContact = newcontact.First
                    Dim rl = From Prof In resp...<discogs>.<parent_label>
                                    Select Prof
                    If rl.Count > 0 Then
                        Dim newlabelparent As New DiscogsNomLabel(rl.<name>.Value)
                        newlabelparent.id = rl.<id>.Value
                        newlabel.labelParent = newlabelparent
                    End If
                    Dim SousLabels = (From sl In resp...<discogs>.<sublabels>
                                        Select sl)
                    For Each i In SousLabels
                        Dim newsouslabel As New DiscogsNomLabel(SousLabels.<name>.Value)
                        newsouslabel.id = SousLabels.<id>.Value
                        newlabel.labelEnfants.Add(newsouslabel)
                    Next
                    Dim Requeteurls = (From url In resp...<discogs>.<urls>
                                        Select url)
                    If Requeteurls IsNot Nothing Then
                        For Each i In Requeteurls
                            newlabel.urls.Add(i.Value)
                        Next
                    End If
                    Dim RequeteListeImages = (From lab In resp...<discogs>.<images>
                                        Select lab)
                    For Each i In RequeteListeImages
                        Dim newDiscogsImage As New DiscogsImage
                        newDiscogsImage.urlImage = i.<uri>.Value
                        newDiscogsImage.hauteur = i.<height>.Value
                        newDiscogsImage.largeur = i.<width>.Value
                        newDiscogsImage.urlImage150 = i.<uri150>.Value
                        If i.<type>.Value = "primary" Then
                            newlabel.images.Insert(0, newDiscogsImage)
                        Else
                            newlabel.images.Add(newDiscogsImage)
                        End If
                    Next
                    idLabelEnCours = newlabel.id
                    labelObject = newlabel
                    Return labelObject
                Catch ex As Exception
                    '  wpfMsgBox.MsgBoxInfo("Erreur DISCOGS", ex.Message, Nothing, "Erreur sur label :" & idLabel)
                    MemorisationErreurDiscogs(ex, idLabel, "Label")
                    Return Nothing
                Finally
                    If reader IsNot Nothing Then reader.Close()
                    If hwebresponse IsNot Nothing Then hwebresponse.Close()
                End Try
            End If
        Else
            releaseObject = Nothing
            Return Nothing
        End If
    End Function
    Public Function GetLabelReleases(ByVal idLabel As DiscogsLabel, ByVal NumPage As Integer) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        If idLabel IsNot Nothing Then
            Try
                Dim resp As XDocument
                hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/labels/" & idLabel.id & "/releases", "page=" & NumPage.ToString & "&per_page=100")
                reader = New IO.StreamReader(hwebresponse.GetResponseStream)
                resp = ConvertReponseXml(reader)
                idLabel.nbrReleases = CInt((From rel In resp...<discogs>.<pagination>.<items>
                                       Select rel.Value).First)
                Dim RequeteReleases = (From rel In resp...<discogs>.<releases>
                                    Select rel)
                For Each i In RequeteReleases
                    Dim newDiscogsReleases As New DiscogsNomRelease
                    newDiscogsReleases.id = i.<id>.Value
                    newDiscogsReleases.titre = i.<title>.Value
                    newDiscogsReleases.format = i.<format>.Value
                    newDiscogsReleases.label = New DiscogsNomLabel(idLabel.nom)
                    newDiscogsReleases.label.catalogue = i.<catno>.Value
                    newDiscogsReleases.label.id = idLabel.id
                    newDiscogsReleases.annee = i.<year>.Value
                    newDiscogsReleases.thumb = i.<thumb>.Value
                    newDiscogsReleases.type = i.<type>.Value
                    If newDiscogsReleases.type = "master" Then newDiscogsReleases.idMainRelease = i.<main_release>.Value
                    idLabel.releases.Add(newDiscogsReleases)
                Next
                Return True
            Catch ex As Exception
                '  wpfMsgBox.MsgBoxInfo("Erreur DISCOGS", ex.Message, Nothing, "Erreur sur releases label :" & idLabel.id)
                MemorisationErreurDiscogs(ex, idLabel.id, "LabelReleases")
                Return False
            Finally
                If reader IsNot Nothing Then reader.Close()
                If hwebresponse IsNot Nothing Then hwebresponse.Close()
            End Try
        Else
            Return False
        End If
    End Function

    Private Shared Function ConvertReponseXml(ByVal ReponseServeur As IO.StreamReader) As XDocument
        Dim ChaineMiseEnForme As String = "{""" & "resp" & """:{""" & "discogs" & """:" & ReponseServeur.ReadToEnd() & "}}"
        Return JsonConvert.DeserializeXNode(ChaineMiseEnForme)
    End Function
    Private Shared Function GetNomFichierXML(ByVal id As String, ByVal typeRequete As String,
                                             Optional ByVal Creation As Boolean = True) As String
        Dim RepDest As String
        RepDest = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) & "\" & GBAU_NOMDOSSIER_STOCKAGEDATA
        If Not Directory.Exists(RepDest) Then Directory.CreateDirectory(RepDest)
        Dim NomFichier As String = String.Format("{0}\{1}{2}_{3}.xml", {RepDest, "Discogs", typeRequete, id})
        If (Not Creation) And (Not File.Exists(NomFichier)) Then
            NomFichier = String.Format("{0}\{1}{2}.xml", {RepDest, "Discogs", typeRequete})
            If Not File.Exists(NomFichier) Then
                Dim s As Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GBAU_NOMRESSOURCE)
                Dim b As FileStream = New FileStream(NomFichier, FileMode.Create)
                s.CopyTo(b)
                b.Close()
            End If
        End If
        Return NomFichier
    End Function
    Public Sub ExportXmlRelease(ByVal id As String)
        Dim NomOrigine As String = GetNomFichierXML(id, "release")
        If id <> "" Then
            Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_STOCKAGEDATA
            If Not Directory.Exists(RepDest) Then Directory.CreateDirectory(RepDest)
            Dim NomFichier As String = RepDest & "\" & "DiscogsRelease.xml"
            File.Copy(NomOrigine, NomFichier, True)
        End If
    End Sub

    Public Function GetXmlErreurFileName() As String
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) & "\" & GBAU_NOMDOSSIER_STOCKAGEDATA
        If Not Directory.Exists(RepDest) Then Directory.CreateDirectory(RepDest)
        Return String.Format("{0}\{1}.xml", {RepDest, "DiscogsError"})
    End Function
    Public Shared Function GetRepertoireImages() As String
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) & "\" & GBAU_NOMDOSSIER_STOCKAGEIMAGES
        If Not Directory.Exists(RepDest) Then Directory.CreateDirectory(RepDest)
        Return RepDest
    End Function
    Private Sub MemorisationErreurDiscogs(ByVal ex As Exception, ByVal id As String, ByVal typeRequete As String)
        Dim NomFichier = GetXmlErreurFileName()
        Try
            Dim newDoc As XDocument = _
                        <?xml version="1.0" encoding="utf-8"?>
                        <DISCOGS>
                            <erreur><%= ex.Message %></erreur>
                            <idrelease><%= id %></idrelease>
                            <typeRequete><%= typeRequete %></typeRequete>
                        </DISCOGS>
            newDoc.Save(NomFichier)
        Catch ex2 As Exception
        End Try
    End Sub

End Class
