Option Compare Text

Imports System.Threading
Imports System.IO
Imports System.Reflection
Imports System.Xml
Imports System.Windows.Threading
Imports System.Management
'***********************************************************************************************
'----------------------------CLASSE PERMETTANT L'ENREGISTREMENT DES INFOS EN TACHE DE FOND------
'***********************************************************************************************
Public Class tagID3Bibliotheque
    Implements iNotifyCollectionUpdate, iNotifyWantListUpdate

    Private Const GBAU_NOMFICHIER_LISTEFICHIERS = "Bibliotheque.xml"
    Private Const GBAU_VERSIONBIBLIOTHEQUE = "1.0.9"
    Private Const GBAU_NOMDOSSIER_IMAGESMP3 = "GBDev\GBPlayer\Images\mp3"

    Public Event AfterUpdate(ByVal MiseAJourOk As Boolean)
    Public Event BeforeUpdate()
    Public Event UpdateDirectoryName(ByVal NewName As String, ByVal oldName As String)
    Public Event IdCollectionAdded(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate) Implements iNotifyCollectionUpdate.IdCollectionAdded
    Public Event IdCollectionRemoved(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate) Implements iNotifyCollectionUpdate.IdCollectionRemoved
    Public Event IdDiscogsCollectionAdded(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate) Implements iNotifyCollectionUpdate.IdDiscogsCollectionAdded
    Public Event IdDiscogsCollectionRemoved(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate) Implements iNotifyCollectionUpdate.IdDiscogsCollectionRemoved
    Public Event IdWantlistAdded(ByVal id As String, ByVal Transmitter As iNotifyWantListUpdate) Implements iNotifyWantListUpdate.IdWantlistAdded
    Public Event IdWantlistRemoved(ByVal id As String, ByVal Transmitter As iNotifyWantListUpdate) Implements iNotifyWantListUpdate.IdWantlistRemoved
    Public Event IdWantlistUpdated(ByVal Document As XmlDocument, ByVal Transmitter As iNotifyWantListUpdate) Implements iNotifyWantListUpdate.IdWantlistUpdated

    Private NewThread As Thread
    Private MiseAjourEnCours As Boolean = False
    Private SearchEnCours As Boolean = False
    Private ListeFichiers As XDocument
    Private ListeRepertoires As XDocument
    Private PathListeFichiers As String
    Private ListeFichiersImagesASupprimer As List(Of String) = New List(Of String)

    Private CustomerUpdateShellEvent As List(Of iNotifyShellUpdate)

    Public Enum EnumShellsModifications
        AjouterRepertoire = 1
        SupprimerRepertoire = 2
        RenommerRepertoire = 3
        AjouterFichier = 4
        SupprimerFichier = 5
        RenommerFichier = 6
        ChangerFichier = 7
    End Enum
    Public Structure MemShellsModifications
        Public Action As EnumShellsModifications
        Public FullPath As String
        Public OldFullPath As String
        Public NonValide As Boolean
        Public Sub New(ByVal NewAction As EnumShellsModifications, ByVal NewFullPath As Object, ByVal NewOldFullPath As Object)
            Action = NewAction
            FullPath = NewFullPath
            OldFullPath = NewOldFullPath
        End Sub
        Public Sub New(ByVal ObjetACloner As MemShellsModifications)
            Action = ObjetACloner.Action
            FullPath = ObjetACloner.FullPath
            OldFullPath = ObjetACloner.OldFullPath
            NonValide = ObjetACloner.NonValide
        End Sub
    End Structure

    Dim WithEvents ShellWatcherFiles As New System.IO.FileSystemWatcher
    Dim WithEvents ShellWatcherFolders As New System.IO.FileSystemWatcher
    Private Property _ListShellUpdate As New List(Of MemShellsModifications)
    Private ShellUpdateThread As Delegate_TacheAsynchrone
    Private ShellUpdateThreadEnCours As Boolean
    Private Delegate Sub Delegate_TacheAsynchrone()
    Private ProcessUpdate As WindowUpdateProgress

    Private Property Racine As String
    Public Property MiseAJourOk As Boolean = False
    Public Property CollectionList As gbListeCollection
    Public Property WantList As userControlWantList
    Public ReadOnly Property IsBusy As Boolean
        Get
            If MiseAjourEnCours Then Return True Else Return False
        End Get
    End Property

    Public Sub New()
        '   db = New BibliothequeDataContext(BibliothequeDataContext.GetDataBaseName())
    End Sub
    Public Sub UpdateBibliotheque(ByVal RacineBibliotheque As String, ByVal Fenetre As MainWindow)
        ProcessUpdate = Fenetre.ProcessMiseAJour
        If Racine <> RacineBibliotheque Or Not MiseAJourOk Then
            MiseAJourOk = False
            If NewThread IsNot Nothing Then
                If NewThread.IsAlive Then NewThread.Abort()
                While (MiseAjourEnCours)
                    Thread.Sleep(100)
                End While
            End If
            Racine = RacineBibliotheque
            PathListeFichiers = Racine & "\" & GBAU_NOMFICHIER_LISTEFICHIERS
            FenetreParente = Fenetre
            If Directory.Exists(RacineBibliotheque) Then 'LANCEMENT DU THREAD EN TACHE DE FOND
                Racine = RacineBibliotheque
                NewThread = New Thread(AddressOf MiseAJourXML)
                NewThread.SetApartmentState(ApartmentState.STA)
                NewThread.Start()
                _ListShellUpdate.Clear()
                ShellWatcherFiles.IncludeSubdirectories = True
                ShellWatcherFiles.Path = Racine
                ShellWatcherFiles.Filter = "*.mp3"
                ShellWatcherFiles.NotifyFilter = NotifyFilters.LastWrite Or NotifyFilters.FileName
                ShellWatcherFiles.EnableRaisingEvents = True
                ShellWatcherFolders.IncludeSubdirectories = True
                ShellWatcherFolders.Path = Racine
                ShellWatcherFolders.NotifyFilter = NotifyFilters.DirectoryName
                ShellWatcherFolders.EnableRaisingEvents = True
            Else
                Racine = ""
                '     NettoyageBibliothèque()
            End If
        End If
    End Sub
    'Procedure de fermeture de la bibliothèque forcant l'arret du thread parallèle de mise à jour
    Public Sub CloseBibliothèque()
        Try
            PurgerImagesASupprimer()
            If MiseAJourOk And (ListeFichiers IsNot Nothing) Then ListeFichiers.Save(PathListeFichiers)
        Catch ex As Exception
        End Try
        If NewThread IsNot Nothing Then
            If NewThread.IsAlive Then NewThread.Abort()
        End If
    End Sub

    '**************************************************************************************************************
    '*****************************GESTION DE LA MISE A JOUR DE LA BIBLIOTHEQUE*************************************
    '**************************************************************************************************************
    Private FenetreParente As MainWindow
    Private Delegate Sub UpdateWindowsDelegate(ByVal NomFichier As String)
    Private NumProcess As Long
    Private CompteurInit As Integer
    Private Sub UpdateWindows(ByVal NomFichier As String)
        If InStr(NomFichier, "#INIT#") > 0 Then
            NumProcess = ProcessUpdate.AddNewProcess(CInt(ExtraitChaine(NomFichier, "#INIT#", "", 6)), NumProcess)
            CompteurInit += 1
        ElseIf NomFichier = "#END#" Then
            CompteurInit -= 1
            If CompteurInit = 0 Then ProcessUpdate.StopProcess(NumProcess)
        Else
            ProcessUpdate.UpdateWindows(NomFichier, NumProcess)
        End If
    End Sub
    Private Sub UpdateWindowsMessage(ByVal NomFichier As String)
        ProcessUpdate.UpdateWindows(NomFichier, NumProcess, True)
    End Sub
    Private Sub MiseAJourXML()
        Try
            RaiseEvent BeforeUpdate()
            FenetreParente.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                          New UpdateWindowsDelegate(AddressOf UpdateWindows), "#INIT#" & "0")
            Dim ListeRepertoiresTemp As XDocument = _
                    <?xml version="1.0" encoding="utf-8"?>
                    <Repertoires>
                    </Repertoires>
            Dim ListeFichiersxmlTemp As XDocument = _
                     <?xml version="1.0" encoding="utf-8"?>
                     <Bibliotheque Version=<%= GBAU_VERSIONBIBLIOTHEQUE %>>
                     </Bibliotheque>

            If Not File.Exists(PathListeFichiers) Then ListeFichiersxmlTemp.Save(PathListeFichiers)
            Try
                ListeFichiers = XDocument.Load(PathListeFichiers)
            Catch
                ListeFichiersxmlTemp.Save(PathListeFichiers)
                ListeFichiers = XDocument.Load(PathListeFichiers)
            End Try
            If ListeFichiers.Root.@Version <> GBAU_VERSIONBIBLIOTHEQUE Then
                ListeFichiersxmlTemp.Save(PathListeFichiers)
                ListeFichiers = XDocument.Load(PathListeFichiers)
                ' PurgerToutesImages()
            End If

            FenetreParente.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                          New UpdateWindowsDelegate(AddressOf UpdateWindowsMessage), "Recherches des répertoires modifiés")
            ListerRepertoires(Racine, ListeRepertoiresTemp)
            FenetreParente.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                          New UpdateWindowsDelegate(AddressOf UpdateWindowsMessage), "Recherche des fichiers modifiés")
            ListerFichiers(ListeRepertoiresTemp, ListeFichiersxmlTemp)

            'DETERMINATION DES FICHIERS MODIFIES
            Dim FichiersModifies As IEnumerable(Of String) = _
                      From i In ListeFichiersxmlTemp.<Bibliotheque>.<Fichier> _
                        Join j In ListeFichiers.<Bibliotheque>.<Fichier> _
                        On i.<Nom>.Value Equals j.<Nom>.Value
                        Where i.<MaJ>.Value <> j.<MaJ>.Value
                        Select i.<Nom>.Value

            'DETERMINATION DES FICHIERS AJOUTES
            Dim ListeFichiersAjoutes = From i In ListeFichiersxmlTemp.<Bibliotheque>.<Fichier>.<Nom>
                                Group Join j In ListeFichiers.<Bibliotheque>.<Fichier>.<Nom>
                                On i.Value Equals j.Value
                                Into RepereExistant = Group
                                Select i.Value, RepereExistant.Ancestors.<Repere>.Value

            Dim FichiersAjoutes As IEnumerable(Of String) = _
                 From i In ListeFichiersAjoutes
                 Where i.Repere <> "NON"
                 Select i.Value

            'TRANSFERT DANS LA NOUVELLE LISTE DE TOUS LES FICHIERS NON MODIFIES
            Dim NouvelleListeFichiers As XDocument = _
                     <?xml version="1.0" encoding="utf-8"?>
                     <Bibliotheque Version=<%= GBAU_VERSIONBIBLIOTHEQUE %>>
                         <%= From i In ListeFichiersxmlTemp.Root.Elements _
                             Join j In ListeFichiers.Root.Elements _
                             On i.<Nom>.Value Equals j.<Nom>.Value
                             Where i.<MaJ>.Value = j.<MaJ>.Value
                             Select New XElement(j) %>
                     </Bibliotheque>

            'AJOUT DES FICHIERS MODIFIES ET AJOUTES
            LockMiseAJourInitiale = True
            AjouterFichiers(FichiersModifies, NouvelleListeFichiers)
            AjouterFichiers(FichiersAjoutes, NouvelleListeFichiers)
            UpdateLists(NouvelleListeFichiers)
            LockMiseAJourInitiale = False

            '     db.SubmitChanges()
            While SearchEnCours
                Thread.Sleep(100)
            End While
            MiseAjourEnCours = True
            FenetreParente.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                          New UpdateWindowsDelegate(AddressOf UpdateWindowsMessage), "Enregistrement de la bibilothèque")
            ListeFichiers = Nothing
            If File.Exists(Path.GetDirectoryName(PathListeFichiers) & "\" &
                                              "~" & Path.GetFileName(PathListeFichiers)) Then _
                My.Computer.FileSystem.DeleteFile(Path.GetDirectoryName(PathListeFichiers) & "\" &
                                                  "~" & Path.GetFileName(PathListeFichiers),
                                                  FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.DeletePermanently)
            My.Computer.FileSystem.RenameFile(PathListeFichiers, "~" & Path.GetFileName(PathListeFichiers))
            NouvelleListeFichiers.Save(PathListeFichiers)
            Try
                ListeFichiers = XDocument.Load(PathListeFichiers)
                My.Computer.FileSystem.DeleteFile(Path.GetDirectoryName(PathListeFichiers) & "\" &
                                                  "~" & Path.GetFileName(PathListeFichiers),
                                                  FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.DeletePermanently)
                ' MiseAJourModifFinDemarrage()
                MiseAJourOk = True
                RaiseEvent AfterUpdate(True)
            Catch ex As Exception
            End Try
        Catch ex As Exception
            RaiseEvent AfterUpdate(False)
        Finally
            MiseAjourEnCours = False
            FenetreParente.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                          New UpdateWindowsDelegate(AddressOf UpdateWindows), "#END#")
        End Try
    End Sub
    'LISTING DES REPERTOIRES DANS LE DOCUMENT XML
    Private Sub ListerRepertoires(ByVal RepertoireBase As String, ByVal Document As XDocument)
        Try
            Dim DirList() As String = Directory.GetDirectories(RepertoireBase)
            Array.ForEach(DirList, Sub(i As String)
                                       If Not (FileIO.FileSystem.GetDirectoryInfo(i).Attributes And IO.FileAttributes.System) = IO.FileAttributes.System And
                                          (FileIO.FileSystem.GetDirectoryInfo(i).Attributes And IO.FileAttributes.Directory) = IO.FileAttributes.Directory Then
                                           Dim RepertoireEnCours As XElement = _
                                               <Repertoire>
                                                   <Nom><%= i %></Nom>
                                                   <Repere>NON</Repere>
                                               </Repertoire>
                                           Document.Root.Add(RepertoireEnCours)
                                           ListerRepertoires(i, Document)
                                       End If
                                   End Sub)

        Catch ex As Exception
        End Try
    End Sub
    'LISTING DES FICHIERS DANS LE DOCUMENT XML
    Private Sub ListerFichiers(ByVal Repertoires As XDocument, ByVal Document As XDocument)
        Try
            For Each Valeur As XElement In Repertoires.Root.Elements
                Dim FilesList() As String = Directory.GetFiles(Valeur.<Nom>.Value, "*.mp3")
                Array.ForEach(FilesList, Sub(i As String)
                                             Dim FichierEnCours As XElement = _
                                                     <Fichier>
                                                         <Nom><%= i %></Nom>
                                                         <MaJ><%= File.GetLastWriteTimeUtc(i).ToString %></MaJ>
                                                         <Repere>NON</Repere>
                                                     </Fichier>
                                             Document.Root.Add(FichierEnCours)
                                         End Sub)
            Next
        Catch ex As Exception
        End Try
    End Sub
    '**************************************************************************************************************
    '*****************************AJOUTS DE NOUVEAUX FICHIERS DANS LA BIBLIOTHEQUE*********************************
    '**************************************************************************************************************
    Dim LockMiseAJourInitiale As Boolean = False
    Dim LockMiseAJourGroupee As Boolean = False
    Private Sub AjouterFichiers(ByVal FichiersAjoutes As IEnumerable(Of String), ByVal Document As XDocument)
        Dim compteur As Integer = 0
        LockMiseAJourGroupee = True
        FenetreParente.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                      New UpdateWindowsDelegate(AddressOf UpdateWindows), "#INIT#" & FichiersAjoutes.Count)
        For Each NomFichierAjoute In FichiersAjoutes
            compteur = compteur + 1
            'If (compteur / 10).ToString = CInt(compteur / 10).ToString Then
            FenetreParente.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                          New UpdateWindowsDelegate(AddressOf UpdateWindows), Path.GetFileName(NomFichierAjoute))
            'End If
            AjouterFichier(NomFichierAjoute, Document)
            Thread.Sleep(25)
        Next
        FenetreParente.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                      New UpdateWindowsDelegate(AddressOf UpdateWindows), "#END#")
        If Not LockMiseAJourInitiale Then UpdateLists(Document)
        LockMiseAJourGroupee = False
    End Sub
    Private Sub UpdateLists(ByVal Document As XDocument, Optional ByVal NewWantList As XmlDocument = Nothing)
        Try
            Dim XCollectionList As XDocument = XDocument.Load(New XmlNodeReader(CollectionList.GetXmlDocument))
            Dim XWantList As XDocument
            If NewWantList IsNot Nothing Then
                XWantList = XDocument.Load(New XmlNodeReader(NewWantList))
            Else
                XWantList = XDocument.Load(New XmlNodeReader(WantList.GetXmlDocument))
            End If
            For Each Disque In From i In Document.<Bibliotheque>.<Fichier> _
                            Where i.<VinylCollection>.Value = True _
                            Or i.<VinylDiscogs>.Value = True _
                            Or i.<VinylEquivalent>.Value = True _
                            Or i.<VinylWanted>.Value = True
                            Select i
                Disque.<VinylCollection>.Value = False
                Disque.<VinylDiscogs>.Value = False
                Disque.<VinylEquivalent>.Value = False
                Disque.<VinylWanted>.Value = False
            Next
            For Each v In From i In Document.<Bibliotheque>.<Fichier> _
                                Where i.<idRelease>.Value <> "" _
                                           Join j In XCollectionList.<GBPLAYER>.<Vinyl> _
                                           On i.<idRelease>.Value Equals j.<id>.Value _
                                           Where j.<VinylDiscogs>.Value = True _
                                           Select i
                v.<VinylDiscogs>.Value = True
            Next v
            For Each v In From i In Document.<Bibliotheque>.<Fichier> _
                             Where i.<idRelease>.Value <> "" _
                                    Join j In XCollectionList.<GBPLAYER>.<Vinyl> _
                                    On i.<idRelease>.Value Equals j.<id>.Value _
                                   Where j.<VinylCollection>.Value = True _
                                   Select i
                v.<VinylCollection>.Value = True
            Next v
            For Each v In From i In Document.<Bibliotheque>.<Fichier> _
                             Where i.<idRelease>.Value <> "" And i.<Album>.Value = "" And i.<VinylCollection>.Value <> True _
                                    Join j In XCollectionList.<GBPLAYER>.<Vinyl> _
                                    On i.<Artiste>.Value Equals j.<Artiste>.Value _
                                    Where j.<Titre>.Value Like Trim(ExtraitChaine(i.<Titre>.Value, "", "[", , True)) & "*" _
                                    Select i
                v.<VinylEquivalent>.Value = True
            Next v
            For Each v In From i In Document.<Bibliotheque>.<Fichier> _
                             Where i.<idRelease>.Value <> "" And i.<Album>.Value <> "" And i.<VinylCollection>.Value <> True _
                                    Join j In XCollectionList.<GBPLAYER>.<Vinyl> _
                                    On i.<Artiste>.Value Equals j.<Artiste>.Value _
                                    Where j.<Format>.Value = "LP" And j.<Titre>.Value = i.<Album>.Value _
                                    Select i
                v.<VinylEquivalent>.Value = True
            Next v
            For Each v In From i In Document.<Bibliotheque>.<Fichier> _
                          Where i.<idRelease>.Value <> "" _
                                 Join j In XWantList.<WANTLIST>.<wants> _
                                 On i.<idRelease>.Value Equals j.<basic_information>.<id>.Value _
                                 Select i
                v.<VinylWanted>.Value = True
            Next v
        Catch ex As Exception
        End Try
    End Sub
    Private Function AjouterFichier(ByVal FichierAjoute As String, ByVal Document As XDocument) As tagID3FilesInfos
        Try
            Dim Fichier As tagID3FilesInfos = New tagID3FilesInfos(FichierAjoute)
            Dim VinylCollection As Boolean
            Dim VinylDiscogs As Boolean
            Dim VinylWanted As Boolean
            Dim VinylEquivalent As Boolean
            If Not LockMiseAJourGroupee Then
                If Fichier.idRelease <> "" Then
                    Dim ListElementCollection = CollectionList.GetXmlDocument.SelectNodes("descendant::Vinyl[id='" & Fichier.idRelease & "']")
                    If ListElementCollection IsNot Nothing Then
                        For Each i As XmlElement In ListElementCollection
                            VinylCollection = VinylCollection Or GetBoolean(i.SelectSingleNode("VinylCollection").InnerText)
                            VinylDiscogs = VinylDiscogs Or GetBoolean(i.SelectSingleNode("VinylDiscogs").InnerText)
                        Next
                        If Not VinylCollection Then
                            Dim ChaineTitre As String = GetXpathString(Trim(ExtraitChaine(Fichier.Titre, "", "[", , True)))
                            'Dim ChaineTitre As String = Trim(ExtraitChaine(Fichier.Titre, "", "[", , True))
                            If Fichier.Album = "" Then
                                Dim ListVinylCollection = CollectionList.GetXmlDocument.SelectSingleNode("descendant::Vinyl" & _
                                                                 "[Artiste='" & GetXpathString(Fichier.Artiste) & "' and " & _
                                                                 "contains(Titre,'" & ChaineTitre & "')]")
                                If ListVinylCollection IsNot Nothing Then
                                    'VinylCollection = GetBoolean(ListVinylCollection.SelectSingleNode("VinylCollection").InnerText)
                                    'VinylDiscogs = GetBoolean(ListVinylCollection.SelectSingleNode("VinylDiscogs").InnerText)
                                    VinylEquivalent = True
                                End If
                            Else
                                Dim ListVinylCollection = CollectionList.GetXmlDocument.SelectSingleNode("descendant::Vinyl" & _
                                                                 "[Artiste='" & GetXpathString(Fichier.Artiste) & "' and Format='LP' and " & _
                                                                 "Titre='" & GetXpathString(Fichier.Album) & "']")
                                If ListVinylCollection IsNot Nothing Then
                                    'VinylCollection = GetBoolean(ListVinylCollection.SelectSingleNode("VinylCollection").InnerText)
                                    'VinylDiscogs = GetBoolean(ListVinylCollection.SelectSingleNode("VinylDiscogs").InnerText)
                                    VinylEquivalent = True
                                End If
                            End If
                        End If
                        Fichier.VinylCollection = VinylCollection
                        Fichier.VinylDiscogs = VinylDiscogs
                        Fichier.VinylEquivalent = VinylEquivalent
                    End If
                    Dim Element = WantList.GetXmlDocument.SelectSingleNode("descendant::wants/basic_information[id='" & Fichier.idRelease & "']")
                    If Element IsNot Nothing Then
                        VinylWanted = True
                        Fichier.VinylWanted = True
                    End If
                Else
                    Fichier.VinylCollection = False
                    Fichier.VinylDiscogs = False
                    Fichier.VinylWanted = False
                    Fichier.VinylEquivalent = False
                End If
            End If
            Dim idImage As String = EnregistrerImage(Fichier.idRelease, Fichier.Image)
            Dim FichierEnCours As XElement = _
                        <Fichier>
                            <Nom><%= FichierAjoute %></Nom>
                            <MaJ><%= File.GetLastWriteTimeUtc(FichierAjoute).ToString %></MaJ>
                            <Repere>NON</Repere>
                            <Id3v2Tag><%= Fichier.Id3v2Tag %></Id3v2Tag>
                            <Id3v1Tag><%= Fichier.Id3v1Tag %></Id3v1Tag>
                            <NomFichier><%= GetFileName(FichierAjoute) %></NomFichier>
                            <Extension><%= GetFileExt(FichierAjoute) %></Extension>
                            <Taille><%= New FileInfo(FichierAjoute).Length.ToString %></Taille>
                            <Repertoire><%= GetFilePath(FichierAjoute) %></Repertoire>
                            <Artiste><%= Fichier.Artiste %></Artiste>
                            <Titre><%= Fichier.Titre %></Titre>
                            <SousTitre><%= Fichier.SousTitre %></SousTitre>
                            <Album><%= Fichier.Album %></Album>
                            <Groupement><%= Fichier.Groupement %></Groupement>
                            <Compositeur><%= Fichier.Compositeur %></Compositeur>
                            <Annee><%= Fichier.Annee %></Annee>
                            <AnneeOrigine><%= Fichier.AnneeOrigine %></AnneeOrigine>
                            <Piste><%= Fichier.Piste %></Piste>
                            <PisteTotale><%= Fichier.PisteTotale %></PisteTotale>
                            <Disque><%= Fichier.Disque %></Disque>
                            <DisqueTotal><%= Fichier.DisqueTotal %></DisqueTotal>
                            <Bpm><%= Fichier.Bpm %></Bpm>
                            <Style><%= Fichier.Style %></Style>
                            <Label><%= Fichier.Label %></Label>
                            <Catalogue><%= Fichier.Catalogue %></Catalogue>
                            <PageWeb><%= Fichier.PageWeb %></PageWeb>
                            <Compilation><%= Fichier.Compilation %></Compilation>
                            <Commentaire><%= Convertutf8(Fichier.Commentaire) %></Commentaire>
                            <TaillePadding><%= Fichier.Padding %></TaillePadding>
                            <Duree><%= Fichier.Duree %></Duree>
                            <idRelease><%= Fichier.idRelease %></idRelease>
                            <idImage><%= idImage %></idImage>
                            <VinylCollection><%= Fichier.VinylCollection %></VinylCollection>
                            <VinylDiscogs><%= Fichier.VinylDiscogs %></VinylDiscogs>
                            <VinylWanted><%= Fichier.VinylWanted %></VinylWanted>
                            <VinylEquivalent><%= Fichier.VinylEquivalent %></VinylEquivalent>
                            <Bitrate><%= Fichier.Bitrate %></Bitrate>
                        </Fichier>
            Fichier.Tag = FichierEnCours
            Document.Root.Add(FichierEnCours)
            Return Fichier
        Catch ex As Exception
            Debug.Print("erreu")
        End Try
    End Function
    Private Function GetBoolean(ByVal Chaine As String) As Boolean
        If Chaine = "True" Then Return True Else Return False
    End Function
    Private Function GetXpathString(ByVal Chaine As String) As String
        Chaine = RemplaceOccurences(Chaine, "'", "&apos")
        Chaine = RemplaceOccurences(Chaine, """", "&quot")
        Return Chaine
    End Function
    Private Sub SupprimerFichier(ByVal FichierSupprime As String)
        PurgerImagesASupprimer()
        Dim ListeFichiersSupprimes = From i In ListeFichiers.<Bibliotheque>.<Fichier>
             Where i.<Nom>.Value = FichierSupprime
             Select i
        Dim ListeASupprimer As New List(Of XElement)
        For Each i In ListeFichiersSupprimes
            ListeASupprimer.Add(i)
        Next
        ListeASupprimer.ForEach(Sub(i As XElement)
                                    ListeFichiersImagesASupprimer.Add(i.<idImage>.Value)
                                    i.Remove()
                                End Sub)
    End Sub
    Private Function Convertutf8(ByVal Chaine As String) As String
        Dim utf8 As New Text.UTF8Encoding()
        Dim Ascii As New Text.ASCIIEncoding
        Try
            If Chaine IsNot Nothing Then
                Return utf8.GetString(Text.Encoding.Convert(Text.ASCIIEncoding.GetEncoding(0),
                                                        Text.UTF8Encoding.GetEncoding(0),
                                                        Ascii.GetBytes(Chaine)))
            Else
                Return ""
            End If
        Catch ex As Exception
            Return ""
        End Try
    End Function
    Dim IndexImage As Integer

    Private Function EnregistrerImage(ByVal id As String, ByVal Data As ImageSource) As String
        Dim PathFichier As String
        Dim idImage As String = id
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_IMAGESMP3
        If Not Directory.Exists(RepDest) Then
            Directory.CreateDirectory(RepDest)
        End If
        If Data IsNot Nothing Then
            If idImage = "" Then
                idImage = "#" & IndexImage
                IndexImage += 1
            End If
            PathFichier = RepDest & "\" & idImage & ".jpg"
            Try
                ' If Not File.Exists(PathFichier) Then
                Do Until Not File.Exists(PathFichier)
                    idImage = idImage & "-" & IndexImage
                    PathFichier = RepDest & "\" & idImage & ".jpg"
                    IndexImage += 1
                Loop
                TagID3.tagID3Object.FonctionUtilite.SaveImage(TagID3.tagID3Object.FonctionUtilite.UploadImage(
                                            TagID3.tagID3Object.FonctionUtilite.CreateResizedImage(Data, 200, 200)), PathFichier)
                Return idImage
                ' End If
            Catch ex As Exception
            End Try
        End If
    End Function
    Private Sub PurgerImagesASupprimer()
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_IMAGESMP3
        Try
            For Each i In ListeFichiersImagesASupprimer
                File.Delete(RepDest & "\" & i & ".jpg")
            Next
            ListeFichiersImagesASupprimer.Clear()
        Catch ex As Exception
            Debug.Print("Erreur purge image bibliotheque")
        End Try
    End Sub
    Private Sub PurgerToutesImages()
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_IMAGESMP3
        Try
            For Each i In Directory.GetFiles(RepDest)
                File.Delete(i)
            Next
        Catch ex As Exception
            Debug.Print("Erreur purge totale images bibliotheque")
        End Try
    End Sub
    'LISTING DES FICHIERS DANS LE DOCUMENT XML
    Public Function PurgerImagesInutiles() As Boolean
        If MiseAJourOk Then
            Dim Document As XDocument = ListerImages()
            For Each Fichier As XElement In From i In ListeFichiers.<Bibliotheque>.<Fichier> _
                                Join j In Document.<ListeFichiersImages>.<Image> _
                                On i.<idImage>.Value Equals j.<Nom>.Value
                                Select j
                Fichier.<Used>.Value = "OUI"
            Next
            For Each Fichier As XElement In From i In Document.<ListeFichiersImages>.<Image> _
                                            Where i.<Used>.Value <> "OUI"
                                            Select i

                If File.Exists(Fichier.<NomComplet>.Value) Then
                    My.Computer.FileSystem.DeleteFile(Fichier.<NomComplet>.Value)
                    Debug.Print(Fichier.<NomComplet>.Value)
                End If
            Next




            Debug.Print(Document.Root.Elements.Count)
            Return True
        End If
        Return False
    End Function
    Private Function ListerImages() As XDocument
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_IMAGESMP3
        Dim NouvelleListeFichiers As XDocument = _
                     <?xml version="1.0" encoding="utf-8"?>
                     <ListeFichiersImages>
                     </ListeFichiersImages>
        Try
            Dim FilesList() As String = Directory.GetFiles(RepDest, "*.*")
            Array.ForEach(FilesList, Sub(i As String)
                                         Dim FichierEnCours As XElement = _
                                                 <Image>
                                                     <NomComplet><%= i %></NomComplet>
                                                     <Nom><%= Path.GetFileNameWithoutExtension(i) %></Nom>
                                                     <Used>NON</Used>
                                                 </Image>
                                         NouvelleListeFichiers.Root.Add(FichierEnCours)
                                     End Sub)

            Return NouvelleListeFichiers
        Catch ex As Exception
        End Try
    End Function
    '**************************************************************************************************************
    '*****************************GESTION DES RECHERCHES DANS LA BIBLIOTHEQUE**************************************
    '**************************************************************************************************************
    Private BaseDeRechercheEnCours As IEnumerable(Of XElement)
    Public Function SearchFilesXml(ByRef Chaine As String, Optional ByVal BaseDeRecherche As IEnumerable(Of XElement) = Nothing) As IEnumerable(Of XElement)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) Then
            If BaseDeRecherche Is Nothing Then
                BaseDeRechercheEnCours = ListeFichiers.<Bibliotheque>.<Fichier>
            Else
                BaseDeRechercheEnCours = BaseDeRecherche
            End If
            SearchEnCours = True
            Dim TabCriteres() As String
            Chaine = Trim(Chaine)
            Dim ResultatFinal As IEnumerable(Of XElement) = Nothing
            Dim Retour As XDocument =
                                    <?xml version="1.0" encoding="utf-8"?>
                                    <ResultatRequete>
                                    </ResultatRequete>
            Dim TabBranche() As String = Split(Chaine, "/")
            Dim MemResultatOR As Boolean = False
            For Each Branche In TabBranche
                If Left(Branche, 1) = "," Then Branche = Right(Branche, Branche.Length - 1)
                'If Left(Chaine, 1) = "," Then Chaine = Right(Chaine, Chaine.Length - 1)
                TabCriteres = Split(Branche, ",") 'Split(Chaine, ",")
                Dim Resultat(TabCriteres.Count - 1) As IEnumerable(Of XElement)
                For j = 0 To TabCriteres.Count - 1
                    Dim NomCritere As String = Trim(ExtraitChaine(TabCriteres(j), "", ":"))
                    Dim ChaineRecherche As String = Trim(ExtraitChaine(TabCriteres(j), ":", "", , True))
                    '  Dim MemChaineRecherche As String = ChaineRecherche
                    '  ChaineRecherche = RemplaceOccurences(ChaineRecherche, "*", "?")
                    '  ChaineRecherche = RemplaceOccurences(ChaineRecherche, "#", "?")
                    ' ChaineRecherche = RemplaceOccurences(ChaineRecherche, "[", "?")
                    ' ChaineRecherche = RemplaceOccurences(ChaineRecherche, "]", "?")
                    ' ChaineRecherche = RemplaceOccurences(ChaineRecherche, "!", "?")
                    ' If ChaineRecherche <> MemChaineRecherche Then If Left(NomCritere, 1) <> "*" Then NomCritere = "*" & NomCritere
                    Select Case NomCritere
                        Case "id", "*id", "+id"
                            Resultat(j) = SearchidXml(ChaineRecherche)
                        Case "d", "dir", "*dir", "*d"
                            Resultat(j) = SearchDirXml(ChaineRecherche)
                        Case "artiste", "*artiste", "+artiste", "a", "+a", "*a"
                            Resultat(j) = SearchArtisteXml(ChaineRecherche, IIf(Left(NomCritere, 1) = "+", True, False), IIf(Left(NomCritere, 1) = "*", False, True))
                        Case "titre", "*titre", "+titre", "t", "*t", "+t"
                            Resultat(j) = SearchTitreXml(ChaineRecherche, IIf(Left(NomCritere, 1) = "+", True, False), IIf(Left(NomCritere, 1) = "*", False, True))
                        Case "label", "*label", "+label", "l", "*l", "+l"
                            Resultat(j) = SearchLabelXml(ChaineRecherche, IIf(Left(NomCritere, 1) = "+", True, False), IIf(Left(NomCritere, 1) = "*", False, True))
                        Case "compositeur", "*compositeur", "+compositeur", "cp", "*cp", "+cp"
                            Resultat(j) = SearchCompositeurXml(ChaineRecherche, IIf(Left(NomCritere, 1) = "+", True, False), IIf(Left(NomCritere, 1) = "*", False, True))
                        Case "groupement", "*groupement", "+groupement", "g", "*g", "+g"
                            Resultat(j) = SearchGroupementXml(ChaineRecherche, IIf(Left(NomCritere, 1) = "+", True, False), IIf(Left(NomCritere, 1) = "*", False, True))
                        Case "style", "+style", "*style", "s", "*s", "+s"
                            Resultat(j) = SearchStyleXml(ChaineRecherche, IIf(Right(NomCritere, 1) = "+", True, False))
                        Case "catalogue", "*catalogue", "+catalogue", "c", "*c", "+c"
                            Resultat(j) = SearchCatalogueXml(ChaineRecherche)
                        Case "annee", "*annee", "+annee", "y", "*y", "+y"
                            Resultat(j) = SearchAnneeXml(ChaineRecherche)
                        Case "piste", "*piste", "+piste", "p", "*p", "+p"
                            Resultat(j) = SearchPisteXml(ChaineRecherche)
                        Case "fichier", "*fichier", "f", "*f"
                            Resultat(j) = SearchNomFichierXml(ChaineRecherche)
                        Case Else
                            Resultat(j) = SearchNomFichierXml(ChaineRecherche)
                    End Select
                Next
                ResultatFinal = Resultat(0)
                For j = 1 To Resultat.Count - 1
                    If Resultat(j) IsNot Nothing Then
                        ResultatFinal = From i In ResultatFinal _
                                        Join v In Resultat(j) _
                                        On i.<Nom>.Value Equals v.<Nom>.Value
                                        Select i
                    Else
                        ResultatFinal = Nothing
                        Exit For
                    End If
                Next
                Retour.Root.Add(ResultatFinal)
            Next
            If TabBranche.Count > 1 Then
                Dim ListeNomsUniques = From i In Retour.Root.Elements Order By i.<Nom>.Value
                                                              Select i.<Nom>.Value
                                                              Distinct
                If ListeNomsUniques.Count > 0 Then
                    ResultatFinal = From i In Retour.Root.Elements _
                                   Join v In ListeNomsUniques _
                                     On i.<Nom>.Value Equals v
                                     Select i
                End If
            End If
            SearchEnCours = False
            Return ResultatFinal
        End If
        Return Nothing
    End Function
    Private Function SearchidXml(ByVal Chaine As String) As IEnumerable(Of XElement)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim Resultat As IEnumerable(Of XElement)
            Resultat = From i In BaseDeRechercheEnCours
                                        Where (i.<idRelease>.Value = Chaine)
                                        Select i
            Return Resultat
        End If
        Return Nothing
    End Function
    Private Function SearchDirXml(ByVal Chaine As String) As IEnumerable(Of XElement)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim Resultat As IEnumerable(Of XElement)
            Dim ChaineLike As String = MiseEnFormeChaineLike(Chaine)
            Resultat = From i In BaseDeRechercheEnCours
                                        Where (i.<Repertoire>.Value Like ChaineLike)
                                        Select i
            Return Resultat
        End If
        Return Nothing
    End Function
    Private Function SearchArtisteXml(ByVal Chaine As String, ByVal RechercheStricte As Boolean, ByVal OrdreRespecte As Boolean) As IEnumerable(Of XElement)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim ResultatFinal As IEnumerable(Of XElement) = Nothing
            Dim ChaineLike As String = MiseEnFormeChaineLike(Chaine)
            If RechercheStricte Then
                ResultatFinal = From i In BaseDeRechercheEnCours
                                        Where (i.<Artiste>.Value = Chaine)
                                        Select i
            ElseIf OrdreRespecte Then
                ResultatFinal = From i In BaseDeRechercheEnCours
                                        Where (i.<Artiste>.Value Like "*" & ChaineLike & "*")
                                        Select i
            Else
                ChaineLike = RemplaceOccurences(ChaineLike, " ", "*")
                Dim TabChaine() As String = Split(ChaineLike)
                Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of XElement)
                If TabChaine.Count > 0 Then
                    Select Case TabChaine.Count
                        Case 1
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Artiste>.Value Like "*" & TabChaine(0) & "*"))
                                                Select i
                        Case 2
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Artiste>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(1) & "*"))
                                                Select i
                        Case 3
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Artiste>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(2) & "*"))
                                                Select i
                        Case 4
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Artiste>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(3) & "*"))
                                                Select i
                        Case 5
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Artiste>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(3) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(4) & "*"))
                                                Select i
                        Case Else
                            For j = 0 To TabChaine.Count - 1
                                Dim NumRequete = j
                                Resultat(NumRequete) = From i In BaseDeRechercheEnCours
                                                Where (i.<Artiste>.Value Like "*" & TabChaine(NumRequete) & "*")
                                                Select i
                            Next
                    End Select
                    ResultatFinal = Resultat(0)
                    If TabChaine.Count > 5 Then
                        For j = 1 To Resultat.Count - 1
                            ResultatFinal = From i In ResultatFinal _
                                            Join v In Resultat(j) _
                                            On i.<Nom>.Value Equals v.<Nom>.Value
                                            Select i
                        Next
                    End If
                End If
            End If
            Return ResultatFinal
        End If
        Return Nothing
    End Function
    Private Function SearchTitreXml(ByVal Chaine As String, ByVal RechercheStricte As Boolean, ByVal OrdreRespecte As Boolean) As IEnumerable(Of XElement)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim ResultatFinal As IEnumerable(Of XElement) = Nothing
            Dim ChaineLike As String = MiseEnFormeChaineLike(Chaine)
            If RechercheStricte Then
                ResultatFinal = From i In BaseDeRechercheEnCours
                                        Where (i.<Titre>.Value = Chaine)
                                        Select i
            ElseIf OrdreRespecte Then
                ResultatFinal = From i In BaseDeRechercheEnCours
                                        Where (i.<Titre>.Value Like "*" & ChaineLike & "*")
                                        Select i
            Else
                ChaineLike = RemplaceOccurences(ChaineLike, " ", "*")
                Dim TabChaine() As String = Split(ChaineLike)
                Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of XElement)
                If TabChaine.Count > 0 Then
                    Select Case TabChaine.Count
                        Case 1
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Titre>.Value Like "*" & TabChaine(0) & "*"))
                                                Select i
                        Case 2
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Titre>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(1) & "*"))
                                                Select i
                        Case 3
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Titre>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(2) & "*"))
                                                Select i
                        Case 4
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Titre>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(3) & "*"))
                                                Select i
                        Case 5
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Titre>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(3) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(4) & "*"))
                                                Select i
                        Case Else
                            For j = 0 To TabChaine.Count - 1
                                Dim NumRequete = j
                                Resultat(NumRequete) = From i In BaseDeRechercheEnCours
                                                Where (i.<Titre>.Value Like "*" & TabChaine(NumRequete) & "*")
                                                Select i
                            Next
                    End Select
                    ResultatFinal = Resultat(0)
                    If TabChaine.Count > 5 Then
                        For j = 1 To Resultat.Count - 1
                            ResultatFinal = From i In ResultatFinal _
                                            Join v In Resultat(j) _
                                            On i.<Nom>.Value Equals v.<Nom>.Value
                                            Select i
                        Next
                    End If
                End If
            End If
            Return ResultatFinal
        End If
        Return Nothing
    End Function
    Private Function SearchLabelXml(ByVal Chaine As String, ByVal RechercheStricte As Boolean, ByVal OrdreRespecte As Boolean) As IEnumerable(Of XElement)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim ResultatFinal As IEnumerable(Of XElement) = Nothing
            Dim ChaineLike As String = MiseEnFormeChaineLike(Chaine)
            If RechercheStricte Then
                ResultatFinal = From i In BaseDeRechercheEnCours
                                        Where (i.<Label>.Value = Chaine)
                                        Select i
            ElseIf OrdreRespecte Then
                ResultatFinal = From i In BaseDeRechercheEnCours
                                        Where (i.<Label>.Value Like "*" & ChaineLike & "*")
                                        Select i
            Else
                ChaineLike = RemplaceOccurences(ChaineLike, " ", "*")
                Dim TabChaine() As String = Split(ChaineLike)
                Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of XElement)
                If TabChaine.Count > 0 Then
                    Select Case TabChaine.Count
                        Case 1
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Label>.Value Like "*" & TabChaine(0) & "*"))
                                                Select i
                        Case 2
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Label>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(1) & "*"))
                                                Select i
                        Case 3
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Label>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(2) & "*"))
                                                Select i
                        Case 4
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Label>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(3) & "*"))
                                                Select i
                        Case 5
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Label>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(3) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(4) & "*"))
                                                Select i
                        Case Else
                            For j = 0 To TabChaine.Count - 1
                                Dim NumRequete = j
                                Resultat(NumRequete) = From i In BaseDeRechercheEnCours
                                                Where (i.<Label>.Value Like "*" & TabChaine(NumRequete) & "*")
                                                Select i
                            Next
                    End Select
                    ResultatFinal = Resultat(0)
                    If TabChaine.Count > 5 Then
                        For j = 1 To Resultat.Count - 1
                            ResultatFinal = From i In ResultatFinal _
                                            Join v In Resultat(j) _
                                            On i.<Nom>.Value Equals v.<Nom>.Value
                                            Select i
                        Next
                    End If
                End If
            End If
            Return ResultatFinal
        End If
        Return Nothing
    End Function
    Private Function SearchCompositeurXml(ByVal Chaine As String, ByVal RechercheStricte As Boolean, ByVal OrdreRespecte As Boolean) As IEnumerable(Of XElement)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim ResultatFinal As IEnumerable(Of XElement) = Nothing
            Dim ChaineLike As String = MiseEnFormeChaineLike(Chaine)
            If RechercheStricte Then
                ResultatFinal = From i In BaseDeRechercheEnCours
                                        Where (i.<Compositeur>.Value = Chaine)
                                        Select i
            ElseIf OrdreRespecte Then
                ResultatFinal = From i In BaseDeRechercheEnCours
                                        Where (i.<Compositeur>.Value Like "*" & ChaineLike & "*")
                                        Select i
            Else
                ChaineLike = RemplaceOccurences(ChaineLike, " ", "*")
                Dim TabChaine() As String = Split(ChaineLike)
                Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of XElement)
                If TabChaine.Count > 0 Then
                    Select Case TabChaine.Count
                        Case 1
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Compositeur>.Value Like "*" & TabChaine(0) & "*"))
                                                Select i
                        Case 2
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Compositeur>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(1) & "*"))
                                                Select i
                        Case 3
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Compositeur>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(2) & "*"))
                                                Select i
                        Case 4
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Compositeur>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(3) & "*"))
                                                Select i
                        Case 5
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Compositeur>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(3) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(4) & "*"))
                                                Select i
                        Case Else
                            For j = 0 To TabChaine.Count - 1
                                Dim NumRequete = j
                                Resultat(NumRequete) = From i In BaseDeRechercheEnCours
                                                Where (i.<Compositeur>.Value Like "*" & TabChaine(NumRequete) & "*")
                                                Select i
                            Next
                    End Select
                    ResultatFinal = Resultat(0)
                    If TabChaine.Count > 5 Then
                        For j = 1 To Resultat.Count - 1
                            ResultatFinal = From i In ResultatFinal _
                                            Join v In Resultat(j) _
                                            On i.<Nom>.Value Equals v.<Nom>.Value
                                            Select i
                        Next
                    End If
                End If
            End If
            Return ResultatFinal
        End If
        Return Nothing
    End Function
    Private Function SearchGroupementXml(ByVal Chaine As String, ByVal RechercheStricte As Boolean, ByVal OrdreRespecte As Boolean) As IEnumerable(Of XElement)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim ResultatFinal As IEnumerable(Of XElement) = Nothing
            Dim ChaineLike As String = MiseEnFormeChaineLike(Chaine)
            If RechercheStricte Then
                ResultatFinal = From i In BaseDeRechercheEnCours
                                        Where (i.<Groupement>.Value = Chaine)
                                        Select i
            ElseIf OrdreRespecte Then
                ResultatFinal = From i In BaseDeRechercheEnCours
                                        Where (i.<Groupement>.Value Like "*" & ChaineLike & "*")
                                        Select i
            Else
                ChaineLike = RemplaceOccurences(ChaineLike, " ", "*")
                Dim TabChaine() As String = Split(ChaineLike)
                Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of XElement)
                If TabChaine.Count > 0 Then
                    Select Case TabChaine.Count
                        Case 1
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Groupement>.Value Like "*" & TabChaine(0) & "*"))
                                                Select i
                        Case 2
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Groupement>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(1) & "*"))
                                                Select i
                        Case 3
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Groupement>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(2) & "*"))
                                                Select i
                        Case 4
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Groupement>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(3) & "*"))
                                                Select i
                        Case 5
                            Resultat(0) = From i In BaseDeRechercheEnCours
                                                Where ((i.<Groupement>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(3) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(4) & "*"))
                                                Select i
                        Case Else
                            For j = 0 To TabChaine.Count - 1
                                Dim NumRequete = j
                                Resultat(NumRequete) = From i In BaseDeRechercheEnCours
                                                Where (i.<Groupement>.Value Like "*" & TabChaine(NumRequete) & "*")
                                                Select i
                            Next
                    End Select
                    ResultatFinal = Resultat(0)
                    If TabChaine.Count > 5 Then
                        For j = 1 To Resultat.Count - 1
                            ResultatFinal = From i In ResultatFinal _
                                            Join v In Resultat(j) _
                                            On i.<Nom>.Value Equals v.<Nom>.Value
                                            Select i
                        Next
                    End If
                End If
            End If
            Return ResultatFinal
        End If
        Return Nothing
    End Function
    Private Function SearchStyleXml(ByVal Chaine As String, ByVal RechercheStricte As Boolean) As IEnumerable(Of XElement)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim Resultat As IEnumerable(Of XElement)
            If RechercheStricte Then
                Resultat = From i In BaseDeRechercheEnCours
                                            Where (i.<Style>.Value = Chaine)
                                            Select i
            Else
                Resultat = From i In BaseDeRechercheEnCours
                                        Where (i.<Style>.Value Like "*" & Chaine & "*")
                                        Select i
            End If
            Return Resultat
        End If
        Return Nothing
    End Function
    Private Function SearchCatalogueXml(ByVal Chaine As String) As IEnumerable(Of XElement)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim ResultatFinal As IEnumerable(Of XElement)
            Dim ChaineLike As String = MiseEnFormeChaineLike(Chaine)
            ChaineLike = RemplaceOccurences(ChaineLike, " ", "*")
            Dim TabChaine() As String = Split(ChaineLike)
            Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of XElement)
            If TabChaine.Count > 0 Then
                Select Case TabChaine.Count
                    Case 1
                        Resultat(0) = From i In BaseDeRechercheEnCours
                                            Where ((i.<Catalogue>.Value Like "*" & TabChaine(0) & "*"))
                                            Select i
                    Case 2
                        Resultat(0) = From i In BaseDeRechercheEnCours
                                            Where ((i.<Catalogue>.Value Like "*" & TabChaine(0) & "*") And
                                                   (i.<Catalogue>.Value Like "*" & TabChaine(1) & "*"))
                                            Select i
                    Case 3
                        Resultat(0) = From i In BaseDeRechercheEnCours
                                            Where ((i.<Catalogue>.Value Like "*" & TabChaine(0) & "*") And
                                                   (i.<Catalogue>.Value Like "*" & TabChaine(1) & "*") And
                                                   (i.<Catalogue>.Value Like "*" & TabChaine(2) & "*"))
                                            Select i
                    Case Else
                        For j = 0 To TabChaine.Count - 1
                            Dim NumRequete = j
                            Resultat(NumRequete) = From i In BaseDeRechercheEnCours
                                            Where (i.<Catalogue>.Value Like "*" & TabChaine(NumRequete) & "*")
                                            Select i
                        Next
                End Select
                ResultatFinal = Resultat(0)
                If TabChaine.Count > 5 Then
                    For j = 1 To Resultat.Count - 1
                        ResultatFinal = From i In ResultatFinal _
                                        Join v In Resultat(j) _
                                        On i.<Nom>.Value Equals v.<Nom>.Value
                                        Select i
                    Next
                End If
                Return ResultatFinal
            End If
        End If
        Return Nothing
    End Function
    Private Function SearchAnneeXml(ByVal Chaine As String) As IEnumerable(Of XElement)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) Then
            Dim Resultat As IEnumerable(Of XElement)
            If Chaine <> "" Then
                If InStr(Chaine, ">") > 0 Then
                    If InStr(Chaine, "=") > 0 Then
                        Resultat = From i In BaseDeRechercheEnCours
                                                    Where (i.<Annee>.Value >= Trim(ExtraitChaine(Chaine, "=", ""))) And i.<Annee>.Value <> ""
                                                    Select i
                    Else
                        Resultat = From i In BaseDeRechercheEnCours
                                                                       Where (i.<Annee>.Value > Trim(ExtraitChaine(Chaine, ">", ""))) And i.<Annee>.Value <> ""
                                                                       Select i
                    End If
                ElseIf InStr(Chaine, "<") > 0 Then
                    If InStr(Chaine, "=") > 0 Then
                        Resultat = From i In BaseDeRechercheEnCours
                                                    Where (i.<Annee>.Value <= Trim(ExtraitChaine(Chaine, "=", ""))) And i.<Annee>.Value <> ""
                                                    Select i
                    Else
                        Resultat = From i In BaseDeRechercheEnCours
                                                                        Where (i.<Annee>.Value < Trim(ExtraitChaine(Chaine, "<", ""))) And i.<Annee>.Value <> ""
                                                                        Select i
                    End If
                Else
                    Resultat = From i In BaseDeRechercheEnCours
                                                Where (i.<Annee>.Value = Chaine)
                                                Select i
                End If
            Else
                Resultat = From i In BaseDeRechercheEnCours
                                            Where (i.<Annee>.Value = "")
                                            Select i
            End If
            Return Resultat
        End If
        Return Nothing
    End Function
    Private Function SearchPisteXml(ByVal Chaine As String) As IEnumerable(Of XElement)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim Resultat As IEnumerable(Of XElement)
            Dim ChaineLike As String = MiseEnFormeChaineLike(Chaine)
            Resultat = From i In BaseDeRechercheEnCours
                                        Where (i.<Piste>.Value Like "*" & ChaineLike & "*")
                                        Select i
            Return Resultat
        End If
        Return Nothing
    End Function
    Private Function SearchNomFichierXml(ByVal ChaineRecherche As String) As IEnumerable(Of XElement)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And ChaineRecherche <> "" Then
            Dim ChaineLike As String = MiseEnFormeChaineLike(ChaineRecherche)
            ChaineLike = RemplaceOccurences(ChaineLike, " ", "*")
            Dim TabChaine() As String = Split(ChaineLike)
            Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of XElement)
            If TabChaine.Count > 0 Then
                Select Case TabChaine.Count
                    Case 1
                        Resultat(0) = From i In BaseDeRechercheEnCours
                                            Where ((i.<NomFichier>.Value Like "*" & TabChaine(0) & "*"))
                                            Select i
                    Case 2
                        Resultat(0) = From i In BaseDeRechercheEnCours
                                            Where ((i.<NomFichier>.Value Like "*" & TabChaine(0) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(1) & "*"))
                                            Select i
                    Case 3
                        Resultat(0) = From i In BaseDeRechercheEnCours
                                            Where ((i.<NomFichier>.Value Like "*" & TabChaine(0) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(1) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(2) & "*"))
                                            Select i
                    Case 4
                        Resultat(0) = From i In BaseDeRechercheEnCours
                                            Where ((i.<NomFichier>.Value Like "*" & TabChaine(0) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(1) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(2) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(3) & "*"))
                                            Select i
                    Case 5
                        Resultat(0) = From i In BaseDeRechercheEnCours
                                            Where ((i.<NomFichier>.Value Like "*" & TabChaine(0) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(1) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(2) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(3) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(4) & "*"))
                                            Select i
                    Case Else
                        For j = 0 To TabChaine.Count - 1
                            Dim NumRequete = j
                            Resultat(NumRequete) = From i In BaseDeRechercheEnCours
                                            Where (i.<NomFichier>.Value Like "*" & TabChaine(NumRequete) & "*")
                                            Select i
                        Next
                End Select
                Dim ResultatFinal As IEnumerable(Of XElement) = Resultat(0)
                If TabChaine.Count > 5 Then
                    For j = 1 To Resultat.Count - 1
                        ResultatFinal = From i In ResultatFinal _
                                        Join v In Resultat(j) _
                                        On i.<Nom>.Value Equals v.<Nom>.Value
                                        Select i
                    Next
                End If
                Return ResultatFinal
            End If
        End If
        Return Nothing
    End Function

    Private Function MiseEnFormeChaineLike(ByVal ChaineOrigine As String) As String
        Dim ChaineMiseEnForme = RemplaceOccurences(ChaineOrigine, "*", "?")
        ChaineMiseEnForme = RemplaceOccurences(ChaineMiseEnForme, "#", "?")
        ChaineMiseEnForme = RemplaceOccurences(ChaineMiseEnForme, "[", "?")
        ChaineMiseEnForme = RemplaceOccurences(ChaineMiseEnForme, "]", "?")
        ChaineMiseEnForme = RemplaceOccurences(ChaineMiseEnForme, "!", "?")
        Return ChaineMiseEnForme
    End Function
    '**************************************************************************************************************
    Public Function SearchFiles(ByRef Chaine As String) As IEnumerable(Of String)
        'PRINCIPE DE CODAGE DE LA REQUETE
        ' syntaxe de la requete 'Nom critere:Valeur cherchee,Autre critere: Valeur cherchee...'
        ' pour une recherche non stricte ajouter '*' en debut du nom du critère
        ' pour une recherche plus stricte ajouter '+' en debut du nom du critère
        ' liste des criteres : id - artiste - titre - label - catalogue - fichier - compositeur - annee - style - piste
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) Then
            SearchEnCours = True
            Dim TabCriteres() As String
            'If Left(Chaine, 1) = "?" Then
            'Chaine = Right(Chaine, Chaine.Length - 1)
            Chaine = Trim(Chaine)
            If Left(Chaine, 1) = "," Then Chaine = Right(Chaine, Chaine.Length - 1)
            TabCriteres = Split(Chaine, ",")
            Dim Resultat(TabCriteres.Count - 1) As IEnumerable(Of String)
            For j = 0 To TabCriteres.Count - 1
                Dim NomCritere As String = Trim(ExtraitChaine(TabCriteres(j), "", ":"))
                Dim ChaineRecherche As String = Trim(ExtraitChaine(TabCriteres(j), ":", "", , True))
                Dim MemChaineRecherche As String = ChaineRecherche
                ChaineRecherche = RemplaceOccurences(ChaineRecherche, "*", "")
                'ChaineRecherche = RemplaceOccurences(ChaineRecherche, "?", "")
                ChaineRecherche = RemplaceOccurences(ChaineRecherche, "#", "")
                ChaineRecherche = RemplaceOccurences(ChaineRecherche, "[", "")
                ChaineRecherche = RemplaceOccurences(ChaineRecherche, "]", "")
                ChaineRecherche = RemplaceOccurences(ChaineRecherche, "!", "")
                If ChaineRecherche <> MemChaineRecherche Then If Left(NomCritere, 1) <> "*" Then NomCritere = "*" & NomCritere
                Select Case NomCritere
                    Case "id", "*id", "+id"
                        Resultat(j) = Searchid(ChaineRecherche)
                    Case "artiste", "*artiste", "+artiste", "a", "+a", "*a"
                        Resultat(j) = SearchArtiste(ChaineRecherche, IIf(Left(NomCritere, 1) = "+", True, False), IIf(Left(NomCritere, 1) = "*", False, True))
                    Case "titre", "*titre", "+titre", "t", "*t", "+t"
                        Resultat(j) = SearchTitre(ChaineRecherche, IIf(Left(NomCritere, 1) = "+", True, False), IIf(Left(NomCritere, 1) = "*", False, True))
                    Case "label", "*label", "+label", "l", "*l", "+l"
                        Resultat(j) = SearchLabel(ChaineRecherche, IIf(Left(NomCritere, 1) = "+", True, False), IIf(Left(NomCritere, 1) = "*", False, True))
                    Case "catalogue", "*catalogue", "+catalogue", "c", "*c", "+c"
                        Resultat(j) = SearchCatalogue(ChaineRecherche)
                    Case "fichier", "*fichier", "f", "*f"
                        Resultat(j) = SearchNomFichier(ChaineRecherche)
                    Case "compositeur", "*compositeur", "+compositeur", "cp", "*cp", "+cp"
                        Resultat(j) = SearchCompositeur(ChaineRecherche, IIf(Left(NomCritere, 1) = "+", True, False), IIf(Left(NomCritere, 1) = "*", False, True))
                    Case "groupement", "*groupement", "+groupement", "g", "*g", "+g"
                        Resultat(j) = SearchGroupement(ChaineRecherche, IIf(Left(NomCritere, 1) = "+", True, False), IIf(Left(NomCritere, 1) = "*", False, True))
                    Case "style", "+style", "*style", "s", "*s", "+s"
                        Resultat(j) = SearchStyle(ChaineRecherche, IIf(Right(NomCritere, 1) = "+", True, False))
                    Case "annee", "*annee", "+annee", "y", "*y", "+y"
                        Resultat(j) = SearchAnnee(ChaineRecherche)
                    Case "piste", "*piste", "+piste", "p", "*p", "+p"
                        Resultat(j) = SearchPiste(ChaineRecherche)
                    Case Else
                        Resultat(j) = SearchNomFichier(ChaineRecherche)
                End Select
            Next
            Dim ResultatFinal As IEnumerable(Of String) = Resultat(0)
            For j = 1 To Resultat.Count - 1
                If Resultat(j) IsNot Nothing Then
                    ResultatFinal = From i In ResultatFinal _
                                    Join v In Resultat(j) _
                                    On i Equals v
                                    Select i
                Else
                    ResultatFinal = Nothing
                    Exit For
                End If
            Next
            SearchEnCours = False
            Return ResultatFinal
            'Else
            '    SearchEnCours = False
            '   Return SearchNomFichier(Chaine)
            'End If
        End If
        Return Nothing
    End Function
    Private Function Searchid(ByVal Chaine As String) As IEnumerable(Of String)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim Resultat As IEnumerable(Of String)
            Resultat = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<idRelease>.Value = Chaine)
                                        Select i.<Nom>.Value
            Return Resultat
        End If
        Return Nothing
    End Function
    Private Function SearchArtiste(ByVal Chaine As String, ByVal RechercheStricte As Boolean, ByVal OrdreRespecte As Boolean) As IEnumerable(Of String)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim ResultatFinal As IEnumerable(Of String) = Nothing
            If RechercheStricte Then
                ResultatFinal = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<Artiste>.Value = Chaine)
                                        Select i.<Nom>.Value
            ElseIf OrdreRespecte Then
                ResultatFinal = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<Artiste>.Value Like "*" & Chaine & "*")
                                        Select i.<Nom>.Value
            Else
                Dim TabChaine() As String = Split(Chaine)
                Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of String)
                If TabChaine.Count > 0 Then
                    Select Case TabChaine.Count
                        Case 1
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Artiste>.Value Like "*" & TabChaine(0) & "*"))
                                                Select i.<Nom>.Value
                        Case 2
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Artiste>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(1) & "*"))
                                                Select i.<Nom>.Value
                        Case 3
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Artiste>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(2) & "*"))
                                                Select i.<Nom>.Value
                        Case 4
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Artiste>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(3) & "*"))
                                                Select i.<Nom>.Value
                        Case 5
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Artiste>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(3) & "*") And
                                                       (i.<Artiste>.Value Like "*" & TabChaine(4) & "*"))
                                                Select i.<Nom>.Value
                        Case Else
                            For j = 0 To TabChaine.Count - 1
                                Dim NumRequete = j
                                Resultat(NumRequete) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where (i.<Artiste>.Value Like "*" & TabChaine(NumRequete) & "*")
                                                Select i.<Nom>.Value
                            Next
                    End Select
                    ResultatFinal = Resultat(0)
                    If TabChaine.Count > 5 Then
                        For j = 1 To Resultat.Count - 1
                            ResultatFinal = From i In ResultatFinal _
                                            Join v In Resultat(j) _
                                            On i Equals v
                                            Select i
                        Next
                    End If
                End If
            End If
            Return ResultatFinal
        End If
        Return Nothing
    End Function
    Private Function SearchTitre(ByVal Chaine As String, ByVal RechercheStricte As Boolean, ByVal OrdreRespecte As Boolean) As IEnumerable(Of String)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim ResultatFinal As IEnumerable(Of String) = Nothing
            If RechercheStricte Then
                ResultatFinal = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<Titre>.Value = Chaine)
                                        Select i.<Nom>.Value
            ElseIf OrdreRespecte Then
                ResultatFinal = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<Titre>.Value Like "*" & Chaine & "*")
                                        Select i.<Nom>.Value
            Else
                Dim TabChaine() As String = Split(Chaine)
                Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of String)
                If TabChaine.Count > 0 Then
                    Select Case TabChaine.Count
                        Case 1
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Titre>.Value Like "*" & TabChaine(0) & "*"))
                                                Select i.<Nom>.Value
                        Case 2
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Titre>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(1) & "*"))
                                                Select i.<Nom>.Value
                        Case 3
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Titre>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(2) & "*"))
                                                Select i.<Nom>.Value
                        Case 4
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Titre>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(3) & "*"))
                                                Select i.<Nom>.Value
                        Case 5
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Titre>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(3) & "*") And
                                                       (i.<Titre>.Value Like "*" & TabChaine(4) & "*"))
                                                Select i.<Nom>.Value
                        Case Else
                            For j = 0 To TabChaine.Count - 1
                                Dim NumRequete = j
                                Resultat(NumRequete) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where (i.<Titre>.Value Like "*" & TabChaine(NumRequete) & "*")
                                                Select i.<Nom>.Value
                            Next
                    End Select
                    ResultatFinal = Resultat(0)
                    If TabChaine.Count > 5 Then
                        For j = 1 To Resultat.Count - 1
                            ResultatFinal = From i In ResultatFinal _
                                            Join v In Resultat(j) _
                                            On i Equals v
                                            Select i
                        Next
                    End If
                End If
            End If
            Return ResultatFinal
        End If
        Return Nothing
    End Function
    Private Function SearchLabel(ByVal Chaine As String, ByVal RechercheStricte As Boolean, ByVal OrdreRespecte As Boolean) As IEnumerable(Of String)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim ResultatFinal As IEnumerable(Of String) = Nothing
            If RechercheStricte Then
                ResultatFinal = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<Label>.Value = Chaine)
                                        Select i.<Nom>.Value
            ElseIf OrdreRespecte Then
                ResultatFinal = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<Label>.Value Like "*" & Chaine & "*")
                                        Select i.<Nom>.Value
            Else
                Dim TabChaine() As String = Split(Chaine)
                Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of String)
                If TabChaine.Count > 0 Then
                    Select Case TabChaine.Count
                        Case 1
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Label>.Value Like "*" & TabChaine(0) & "*"))
                                                Select i.<Nom>.Value
                        Case 2
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Label>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(1) & "*"))
                                                Select i.<Nom>.Value
                        Case 3
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Label>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(2) & "*"))
                                                Select i.<Nom>.Value
                        Case 4
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Label>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(3) & "*"))
                                                Select i.<Nom>.Value
                        Case 5
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Label>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(3) & "*") And
                                                       (i.<Label>.Value Like "*" & TabChaine(4) & "*"))
                                                Select i.<Nom>.Value
                        Case Else
                            For j = 0 To TabChaine.Count - 1
                                Dim NumRequete = j
                                Resultat(NumRequete) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where (i.<Label>.Value Like "*" & TabChaine(NumRequete) & "*")
                                                Select i.<Nom>.Value
                            Next
                    End Select
                    ResultatFinal = Resultat(0)
                    If TabChaine.Count > 5 Then
                        For j = 1 To Resultat.Count - 1
                            ResultatFinal = From i In ResultatFinal _
                                            Join v In Resultat(j) _
                                            On i Equals v
                                            Select i
                        Next
                    End If
                End If
            End If
            Return ResultatFinal
        End If
        Return Nothing
    End Function
    Private Function SearchCompositeur(ByVal Chaine As String, ByVal RechercheStricte As Boolean, ByVal OrdreRespecte As Boolean) As IEnumerable(Of String)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim ResultatFinal As IEnumerable(Of String) = Nothing
            If RechercheStricte Then
                ResultatFinal = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<Compositeur>.Value = Chaine)
                                        Select i.<Nom>.Value
            ElseIf OrdreRespecte Then
                ResultatFinal = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<Compositeur>.Value Like "*" & Chaine & "*")
                                        Select i.<Nom>.Value
            Else
                Dim TabChaine() As String = Split(Chaine)
                Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of String)
                If TabChaine.Count > 0 Then
                    Select Case TabChaine.Count
                        Case 1
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Compositeur>.Value Like "*" & TabChaine(0) & "*"))
                                                Select i.<Nom>.Value
                        Case 2
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Compositeur>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(1) & "*"))
                                                Select i.<Nom>.Value
                        Case 3
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Compositeur>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(2) & "*"))
                                                Select i.<Nom>.Value
                        Case 4
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Compositeur>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(3) & "*"))
                                                Select i.<Nom>.Value
                        Case 5
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Compositeur>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(3) & "*") And
                                                       (i.<Compositeur>.Value Like "*" & TabChaine(4) & "*"))
                                                Select i.<Nom>.Value
                        Case Else
                            For j = 0 To TabChaine.Count - 1
                                Dim NumRequete = j
                                Resultat(NumRequete) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where (i.<Compositeur>.Value Like "*" & TabChaine(NumRequete) & "*")
                                                Select i.<Nom>.Value
                            Next
                    End Select
                    ResultatFinal = Resultat(0)
                    If TabChaine.Count > 5 Then
                        For j = 1 To Resultat.Count - 1
                            ResultatFinal = From i In ResultatFinal _
                                            Join v In Resultat(j) _
                                            On i Equals v
                                            Select i
                        Next
                    End If
                End If
            End If
            Return ResultatFinal
        End If
        Return Nothing
    End Function
    Private Function SearchGroupement(ByVal Chaine As String, ByVal RechercheStricte As Boolean, ByVal OrdreRespecte As Boolean) As IEnumerable(Of String)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim ResultatFinal As IEnumerable(Of String) = Nothing
            If RechercheStricte Then
                ResultatFinal = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<Groupement>.Value = Chaine)
                                        Select i.<Nom>.Value
            ElseIf OrdreRespecte Then
                ResultatFinal = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<Groupement>.Value Like "*" & Chaine & "*")
                                        Select i.<Nom>.Value
            Else
                Dim TabChaine() As String = Split(Chaine)
                Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of String)
                If TabChaine.Count > 0 Then
                    Select Case TabChaine.Count
                        Case 1
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Groupement>.Value Like "*" & TabChaine(0) & "*"))
                                                Select i.<Nom>.Value
                        Case 2
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Groupement>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(1) & "*"))
                                                Select i.<Nom>.Value
                        Case 3
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Groupement>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(2) & "*"))
                                                Select i.<Nom>.Value
                        Case 4
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Groupement>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(3) & "*"))
                                                Select i.<Nom>.Value
                        Case 5
                            Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where ((i.<Groupement>.Value Like "*" & TabChaine(0) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(1) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(2) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(3) & "*") And
                                                       (i.<Groupement>.Value Like "*" & TabChaine(4) & "*"))
                                                Select i.<Nom>.Value
                        Case Else
                            For j = 0 To TabChaine.Count - 1
                                Dim NumRequete = j
                                Resultat(NumRequete) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                                Where (i.<Groupement>.Value Like "*" & TabChaine(NumRequete) & "*")
                                                Select i.<Nom>.Value
                            Next
                    End Select
                    ResultatFinal = Resultat(0)
                    If TabChaine.Count > 5 Then
                        For j = 1 To Resultat.Count - 1
                            ResultatFinal = From i In ResultatFinal _
                                            Join v In Resultat(j) _
                                            On i Equals v
                                            Select i
                        Next
                    End If
                End If
            End If
            Return ResultatFinal
        End If
        Return Nothing
    End Function
    Private Function SearchStyle(ByVal Chaine As String, ByVal RechercheStricte As Boolean) As IEnumerable(Of String)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim Resultat As IEnumerable(Of String)
            If RechercheStricte Then
                Resultat = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                            Where (i.<Style>.Value = Chaine)
                                            Select i.<Nom>.Value
            Else
                Resultat = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<Style>.Value Like "*" & Chaine & "*")
                                        Select i.<Nom>.Value
            End If
            Return Resultat
        End If
        Return Nothing
    End Function
    Private Function SearchCatalogue(ByVal Chaine As String) As IEnumerable(Of String)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim ResultatFinal As IEnumerable(Of String)
            Dim TabChaine() As String = Split(Chaine)
            Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of String)
            If TabChaine.Count > 0 Then
                Select Case TabChaine.Count
                    Case 1
                        Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                            Where ((i.<Catalogue>.Value Like "*" & TabChaine(0) & "*"))
                                            Select i.<Nom>.Value
                    Case 2
                        Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                            Where ((i.<Catalogue>.Value Like "*" & TabChaine(0) & "*") And
                                                   (i.<Catalogue>.Value Like "*" & TabChaine(1) & "*"))
                                            Select i.<Nom>.Value
                    Case 3
                        Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                            Where ((i.<Catalogue>.Value Like "*" & TabChaine(0) & "*") And
                                                   (i.<Catalogue>.Value Like "*" & TabChaine(1) & "*") And
                                                   (i.<Catalogue>.Value Like "*" & TabChaine(2) & "*"))
                                            Select i.<Nom>.Value
                    Case Else
                        For j = 0 To TabChaine.Count - 1
                            Dim NumRequete = j
                            Resultat(NumRequete) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                            Where (i.<Catalogue>.Value Like "*" & TabChaine(NumRequete) & "*")
                                            Select i.<Nom>.Value
                        Next
                End Select
                ResultatFinal = Resultat(0)
                If TabChaine.Count > 5 Then
                    For j = 1 To Resultat.Count - 1
                        ResultatFinal = From i In ResultatFinal _
                                        Join v In Resultat(j) _
                                        On i Equals v
                                        Select i
                    Next
                End If
                Return ResultatFinal
            End If
        End If
        Return Nothing
    End Function
    Private Function SearchAnnee(ByVal Chaine As String) As IEnumerable(Of String)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim Resultat As IEnumerable(Of String)
            Resultat = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<Annee>.Value = Chaine)
                                        Select i.<Nom>.Value
            Return Resultat
        End If
        Return Nothing
    End Function
    Private Function SearchPiste(ByVal Chaine As String) As IEnumerable(Of String)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Chaine <> "" Then
            Dim Resultat As IEnumerable(Of String)
            Resultat = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                        Where (i.<Piste>.Value Like "*" & Chaine & "*")
                                        Select i.<Nom>.Value
            Return Resultat
        End If
        Return Nothing
    End Function
    Private Function SearchNomFichier(ByVal ChaineRecherche As String) As IEnumerable(Of String)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And ChaineRecherche <> "" Then
            ChaineRecherche = RemplaceOccurences(ChaineRecherche, "*", " ")
            ChaineRecherche = RemplaceOccurences(ChaineRecherche, "?", " ")
            ChaineRecherche = RemplaceOccurences(ChaineRecherche, "#", " ")
            ChaineRecherche = RemplaceOccurences(ChaineRecherche, "[", " ")
            ChaineRecherche = RemplaceOccurences(ChaineRecherche, "]", " ")
            ChaineRecherche = RemplaceOccurences(ChaineRecherche, "!", " ")
            Dim TabChaine() As String = Split(ChaineRecherche)
            Dim Resultat(TabChaine.Count - 1) As IEnumerable(Of String)
            If TabChaine.Count > 0 Then
                Select Case TabChaine.Count
                    Case 1
                        Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                            Where ((i.<NomFichier>.Value Like "*" & TabChaine(0) & "*"))
                                            Select i.<Nom>.Value
                    Case 2
                        Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                            Where ((i.<NomFichier>.Value Like "*" & TabChaine(0) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(1) & "*"))
                                            Select i.<Nom>.Value
                    Case 3
                        Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                            Where ((i.<NomFichier>.Value Like "*" & TabChaine(0) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(1) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(2) & "*"))
                                            Select i.<Nom>.Value
                    Case 4
                        Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                            Where ((i.<NomFichier>.Value Like "*" & TabChaine(0) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(1) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(2) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(3) & "*"))
                                            Select i.<Nom>.Value
                    Case 5
                        Resultat(0) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                            Where ((i.<NomFichier>.Value Like "*" & TabChaine(0) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(1) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(2) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(3) & "*") And
                                                   (i.<NomFichier>.Value Like "*" & TabChaine(4) & "*"))
                                            Select i.<Nom>.Value
                    Case Else
                        For j = 0 To TabChaine.Count - 1
                            Dim NumRequete = j
                            Resultat(NumRequete) = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                            Where (i.<NomFichier>.Value Like "*" & TabChaine(NumRequete) & "*")
                                            Select i.<Nom>.Value
                        Next
                End Select
                Dim ResultatFinal As IEnumerable(Of String) = Resultat(0)
                If TabChaine.Count > 5 Then
                    For j = 1 To Resultat.Count - 1
                        ResultatFinal = From i In ResultatFinal _
                                        Join v In Resultat(j) _
                                        On i Equals v
                                        Select i
                    Next
                End If
                Return ResultatFinal
            End If
        End If
        Return Nothing
    End Function

    '**************************************************************************************************************
    '*****************************GESTION DES MISES A JOURS********************************************************
    '**************************************************************************************************************
    Public Function SubscribeUpdateShellEvent(ByVal Element As iNotifyShellUpdate) As Boolean
        If CustomerUpdateShellEvent Is Nothing Then CustomerUpdateShellEvent = New List(Of iNotifyShellUpdate)
        If CustomerUpdateShellEvent.IndexOf(Element) = -1 Then
            Element.BibliothequeLiee = Me
            CustomerUpdateShellEvent.Add(Element)
            Return True
        End If
        Return False
    End Function
    Public Function UnSubscribeUpdateShellEvent(ByVal Element As iNotifyShellUpdate) As Boolean
        If CustomerUpdateShellEvent IsNot Nothing Then
            If CustomerUpdateShellEvent.IndexOf(Element) <> -1 Then
                Element.BibliothequeLiee = Nothing
                CustomerUpdateShellEvent.Remove(Element)
                Return True
            End If
        End If
        Return False
    End Function
    Sub ShellWatcherFilesCreated(ByVal e As FileSystemEventArgs, ByVal Infos As tagID3FilesInfos)
        CustomerUpdateShellEvent.ForEach(Sub(i As iNotifyShellUpdate)
                                             Debug.Print("Creation : " & e.FullPath)
                                             If (i.Filter <> "") And (Infos IsNot Nothing) Then
                                                 Dim Coll As List(Of XElement) = New List(Of XElement)
                                                 Coll.Add(CType(Infos.Tag, XElement))
                                                 If SearchFilesXml(i.Filter, Coll).Count > 0 Then
                                                     i.NotifyShellWatcherFilesCreated(e, Infos)
                                                 End If
                                                 Exit Sub
                                             End If
                                             i.NotifyShellWatcherFilesCreated(e, Infos)
                                         End Sub)
    End Sub
    Sub ShellWatcherFilesDeleted(ByVal e As FileSystemEventArgs)
        CustomerUpdateShellEvent.ForEach(Sub(i As iNotifyShellUpdate)
                                             Debug.Print("Delete : " & e.FullPath)
                                             i.NotifyShellWatcherFilesDeleted(e)
                                         End Sub)
    End Sub
    Sub ShellWatcherFilesChanged(ByVal e As FileSystemEventArgs, ByVal Infos As tagID3FilesInfos)
        CustomerUpdateShellEvent.ForEach(Sub(i As iNotifyShellUpdate)
                                             Debug.Print("Changement : " & e.FullPath)
                                             If (i.Filter <> "") And (Infos IsNot Nothing) Then
                                                 Dim Coll As List(Of XElement) = New List(Of XElement)
                                                 Coll.Add(CType(Infos.Tag, XElement))
                                                 If SearchFilesXml(i.Filter, Coll).Count > 0 Then
                                                     i.NotifyShellWatcherFilesChanged(e, Infos)
                                                 Else
                                                     i.NotifyShellWatcherFilesDeleted(e)
                                                 End If
                                                 Exit Sub
                                             End If
                                             i.NotifyShellWatcherFilesChanged(e, Infos)
                                         End Sub)
    End Sub
    Sub ShellWatcherFilesRenamed(ByVal e As RenamedEventArgs, ByVal Infos As tagID3FilesInfos)
        CustomerUpdateShellEvent.ForEach(Sub(i As iNotifyShellUpdate)
                                             Debug.Print("Renome : " & e.FullPath)
                                             If (i.Filter <> "") And (Infos IsNot Nothing) Then
                                                 Dim Coll As List(Of XElement) = New List(Of XElement)
                                                 Coll.Add(CType(Infos.Tag, XElement))
                                                 If SearchFilesXml(i.Filter, Coll).Count > 0 Then
                                                     i.NotifyShellWatcherFilesRenamed(e, Infos)
                                                 Else
                                                     Dim NewE As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Deleted,
                                                                                                               Path.GetDirectoryName(e.OldFullPath),
                                                                                                               Path.GetFileName(e.OldName))
                                                     i.NotifyShellWatcherFilesDeleted(NewE)
                                                 End If
                                                 Exit Sub
                                             End If
                                             i.NotifyShellWatcherFilesRenamed(e, Infos)
                                         End Sub)
    End Sub

    Public Function NotifyAddIdCollection(ByVal Id As String) As Boolean Implements iNotifyCollectionUpdate.NotifyAddIdCollection
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Id <> "" Then
            For Each XFichier In From i In ListeFichiers.<Bibliotheque>.<Fichier> _
                            Where i.<idRelease>.Value = Id _
                            Select i
                Dim Infos As tagID3FilesInfos = New tagID3FilesInfos(XFichier.<Nom>.Value)
                XFichier.<VinylCollection>.Value = True
                Infos.VinylCollection = XFichier.<VinylCollection>.Value
                Infos.VinylDiscogs = XFichier.<VinylDiscogs>.Value
                Infos.VinylWanted = XFichier.<VinylWanted>.Value
                Infos.VinylEquivalent = XFichier.<VinylEquivalent>.Value
                Infos.Tag = XFichier
                Dim e As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Changed,
                                                                                  XFichier.<Repertoire>.Value,
                                                                                 Path.GetFileName(XFichier.<Nom>.Value))
                ShellWatcherFilesChanged(e, Infos)
            Next XFichier
            Return True
        End If
    End Function
    Public Function NotifyRemoveIdCollection(ByVal id As String) As Boolean Implements iNotifyCollectionUpdate.NotifyRemoveIdCollection
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And id <> "" Then
            For Each XFichier In From i In ListeFichiers.<Bibliotheque>.<Fichier> _
                            Where i.<idRelease>.Value = id _
                            Select i
                Dim Infos As tagID3FilesInfos = New tagID3FilesInfos(XFichier.<Nom>.Value)
                XFichier.<VinylCollection>.Value = False
                Infos.VinylCollection = XFichier.<VinylCollection>.Value
                Infos.VinylDiscogs = XFichier.<VinylDiscogs>.Value
                Infos.VinylWanted = XFichier.<VinylWanted>.Value
                Infos.VinylEquivalent = XFichier.<VinylEquivalent>.Value
                Infos.Tag = XFichier
                Dim e As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Changed,
                                                                                  XFichier.<Repertoire>.Value,
                                                                                 Path.GetFileName(XFichier.<Nom>.Value))
                ShellWatcherFilesChanged(e, Infos)
            Next XFichier
            Return True
        End If
    End Function
    Public Function NotifyAddIdDiscogsCollection(ByVal Id As String) As Boolean Implements iNotifyCollectionUpdate.NotifyAddIdDiscogsCollection
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Id <> "" Then
            For Each XFichier In From i In ListeFichiers.<Bibliotheque>.<Fichier> _
                            Where i.<idRelease>.Value = Id _
                            Select i
                Dim Infos As tagID3FilesInfos = New tagID3FilesInfos(XFichier.<Nom>.Value)
                XFichier.<VinylDiscogs>.Value = True
                Infos.VinylCollection = XFichier.<VinylCollection>.Value
                Infos.VinylDiscogs = XFichier.<VinylDiscogs>.Value
                Infos.VinylWanted = XFichier.<VinylWanted>.Value
                Infos.VinylEquivalent = XFichier.<VinylEquivalent>.Value
                Infos.Tag = XFichier
                Dim e As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Changed,
                                                                                  XFichier.<Repertoire>.Value,
                                                                                 Path.GetFileName(XFichier.<Nom>.Value))
                ShellWatcherFilesChanged(e, Infos)
            Next XFichier
            Return True
        End If
    End Function
    Public Function NotifyRemoveIdDiscogsCollection(ByVal id As String) As Boolean Implements iNotifyCollectionUpdate.NotifyRemoveIdDiscogsCollection
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And id <> "" Then
            For Each XFichier In From i In ListeFichiers.<Bibliotheque>.<Fichier> _
                            Where i.<idRelease>.Value = id And id <> "" _
                            Select i
                Dim Infos As tagID3FilesInfos = New tagID3FilesInfos(XFichier.<Nom>.Value)
                XFichier.<VinylDiscogs>.Value = False
                Infos.VinylCollection = XFichier.<VinylCollection>.Value
                Infos.VinylDiscogs = XFichier.<VinylDiscogs>.Value
                Infos.VinylWanted = XFichier.<VinylWanted>.Value
                Infos.VinylEquivalent = XFichier.<VinylEquivalent>.Value
                Infos.Tag = XFichier
                Dim e As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Changed,
                                                                                  XFichier.<Repertoire>.Value,
                                                                                 Path.GetFileName(XFichier.<Nom>.Value))
                ShellWatcherFilesChanged(e, Infos)
            Next XFichier
            Return True
        End If
    End Function
    Public Function NotifyAddIdWantlist(ByVal Id As String) As Boolean Implements iNotifyWantListUpdate.NotifyAddIdWantlist
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And Id <> "" Then
            For Each XFichier In From i In ListeFichiers.<Bibliotheque>.<Fichier> _
                            Where i.<idRelease>.Value = Id _
                            Select i
                Dim Infos As tagID3FilesInfos = New tagID3FilesInfos(XFichier.<Nom>.Value)
                XFichier.<VinylWanted>.Value = True
                Infos.VinylCollection = XFichier.<VinylCollection>.Value
                Infos.VinylDiscogs = XFichier.<VinylDiscogs>.Value
                Infos.VinylWanted = XFichier.<VinylWanted>.Value
                Infos.VinylEquivalent = XFichier.<VinylEquivalent>.Value
                Infos.Tag = XFichier
                Dim e As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Changed,
                                                                                  XFichier.<Repertoire>.Value,
                                                                                 Path.GetFileName(XFichier.<Nom>.Value))
                ShellWatcherFilesChanged(e, Infos)
            Next XFichier
            Return True
        End If
    End Function
    Public Function NotifyRemoveIdWantlist(ByVal id As String) As Boolean Implements iNotifyWantListUpdate.NotifyRemoveIdWantlist
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) And id <> "" Then
            For Each XFichier In From i In ListeFichiers.<Bibliotheque>.<Fichier> _
                            Where i.<idRelease>.Value = id _
                            Select i
                Dim Infos As tagID3FilesInfos = New tagID3FilesInfos(XFichier.<Nom>.Value)
                XFichier.<VinylWanted>.Value = False
                Infos.VinylCollection = XFichier.<VinylCollection>.Value
                Infos.VinylDiscogs = XFichier.<VinylDiscogs>.Value
                Infos.VinylWanted = XFichier.<VinylWanted>.Value
                Infos.VinylEquivalent = XFichier.<VinylEquivalent>.Value
                Infos.Tag = XFichier
                Dim e As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Changed,
                                                                                  XFichier.<Repertoire>.Value,
                                                                                 Path.GetFileName(XFichier.<Nom>.Value))
                ShellWatcherFilesChanged(e, Infos)
            Next XFichier
            Return True
        End If
    End Function
    Public Function NotifyUpdateWantList(ByVal Document As XmlDocument) As Boolean Implements iNotifyWantListUpdate.NotifyUpdateWantList
        If MiseAJourOk Then
            UpdateLists(ListeFichiers, Document)
            RaiseEvent AfterUpdate(MiseAJourOk)
        End If
        Return True
    End Function
    '*****************************GESTION DE LA MISE A JOUR AUTOMATIQUE DE LA LISTE********************************
    '**************************************************************************************************************
    'PROCEDURE DE MISE A JOUR DE LA LISTE DES FICHIERS
    'Traitement du message lors de la notification de creation d'un fichier
    Dim ExFichierSupprime As tagID3FilesInfosDO = Nothing
    Delegate Sub CallBackToFileSystemWatcher(ByVal e As FileSystemEventArgs)
    Delegate Sub CallBackToFileSystemWatcherRenamed(ByVal e As System.IO.RenamedEventArgs)
    Private Sub ShellWatcherFiles_Created(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles ShellWatcherFiles.Created
        ShellUpdateRequest(EnumShellsModifications.AjouterFichier, e.FullPath, "")
    End Sub
    Private Sub NotifyShellWatcherFilesCreated(ByVal e As FileSystemEventArgs)
        If Path.GetDirectoryName(e.FullPath) <> Racine And Path.GetExtension(e.FullPath) = ".mp3" Then
            Debug.Print("Created : " & e.FullPath)
            Dim infos As tagID3FilesInfos = AjouterFichier(e.FullPath, ListeFichiers)
            If infos IsNot Nothing Then ShellWatcherFilesCreated(e, infos)
        End If
    End Sub
    'Traitement du message lors de la notification d'effacement d'un fichier
    Private Sub ShellWatcherFiles_Deleted(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles ShellWatcherFiles.Deleted
        ShellUpdateRequest(EnumShellsModifications.SupprimerFichier, e.FullPath, "")
    End Sub
    Private Sub NotifyShellWatcherFilesDeleted(ByVal e As FileSystemEventArgs)
        If Path.GetExtension(e.FullPath) = ".mp3" Then
            Debug.Print("Deleted : " & e.FullPath)
            SupprimerFichier(e.FullPath)
            ShellWatcherFilesDeleted(e)
        End If
    End Sub
    'Traitement du message lors de la notification de modification d'un fichier
    Private Sub ShellWatcherFiles_Changed(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles ShellWatcherFiles.Changed
        ShellUpdateRequest(EnumShellsModifications.ChangerFichier, e.FullPath, "")
    End Sub
    Private Sub NotifyShellWatcherFilesChanged(ByVal e As FileSystemEventArgs)
        If Path.GetExtension(e.FullPath) = ".mp3" Then
            Debug.Print("Changed : " & e.FullPath)
            SupprimerFichier(e.FullPath)
            Dim infos As tagID3FilesInfos = AjouterFichier(e.FullPath, ListeFichiers)
            If infos IsNot Nothing Then ShellWatcherFilesChanged(e, infos)
        End If
    End Sub
    'Traitement du message lors de la notification de renommage d'un fichier
    Private Sub ShellWatcherFiles_Renamed(ByVal sender As Object, ByVal e As System.IO.RenamedEventArgs) Handles ShellWatcherFiles.Renamed
        ShellUpdateRequest(EnumShellsModifications.RenommerFichier, e.FullPath, e.OldFullPath)
    End Sub
    Private Sub NotifyShellWatcherFilesRenamed(ByVal e As System.IO.RenamedEventArgs)
        If Path.GetExtension(e.FullPath) = ".mp3" Then
            If Path.GetExtension(e.OldFullPath) = ".~p3" And (Path.GetFileNameWithoutExtension(e.OldFullPath) = Path.GetFileNameWithoutExtension(e.FullPath)) Then
                NotifyShellWatcherFilesChanged(e)
            Else
                Dim FileSup As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(e.OldFullPath), Path.GetFileName(e.OldName))
                Dim FileAjoute As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(e.FullPath), Path.GetFileName(e.Name))
                SupprimerFichier(e.OldFullPath)
                Dim infos As tagID3FilesInfos = AjouterFichier(e.FullPath, ListeFichiers)
                If infos IsNot Nothing Then ShellWatcherFilesRenamed(e, infos)
            End If
        End If
    End Sub

    'Traitement du message lors de la notification de creation d'un repertoire
    Private Sub ShellWatcherFolders_Created(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles ShellWatcherFolders.Created
        ShellUpdateRequest(EnumShellsModifications.AjouterRepertoire, e.FullPath, "")
    End Sub
    Private Sub NotifyShellWatcherFoldersCreated(ByVal e As FileSystemEventArgs)
        Dim ListeRepertoiresTemp As XDocument = _
                <?xml version="1.0" encoding="utf-8"?>
                <Repertoires>
                    <Repertoire>
                        <Nom><%= e.FullPath %></Nom>
                        <Repere>NON</Repere>
                    </Repertoire>
                </Repertoires>
        Dim ListeFichiersxmlTemp As XDocument = _
                 <?xml version="1.0" encoding="utf-8"?>
                 <Bibliotheque Version=<%= GBAU_VERSIONBIBLIOTHEQUE %>>
                 </Bibliotheque>
        ListerRepertoires(e.FullPath, ListeRepertoiresTemp)
        ListerFichiers(ListeRepertoiresTemp, ListeFichiersxmlTemp)
        Dim ListeFichiersAjoutes = From i In ListeFichiersxmlTemp.<Bibliotheque>.<Fichier>
                                    Select i.<Nom>.Value

        For Each i As String In ListeFichiersAjoutes
            Dim NomACreer As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(i), Path.GetFileName(i))
            NotifyShellWatcherFilesCreated(NomACreer)
        Next
        ' AjouterFichiers(ListeFichiersAjoutes, ListeFichiers)
        'FenetreParente.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
        '              New UpdateWindowsDelegate(AddressOf UpdateWindows), -1, 0, "", "")
    End Sub
    'Traitement du message lors de la notification d'effacement d'un repertoire
    Private Sub ShellWatcherFolders_Deleted(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles ShellWatcherFolders.Deleted
        ShellUpdateRequest(EnumShellsModifications.SupprimerRepertoire, e.FullPath, "")
    End Sub
    Private Sub NotifyShellWatcherFoldersDeleted(ByVal e As FileSystemEventArgs)
        Dim ChaineLike As String = MiseEnFormeChaineLike(e.FullPath)
        Dim ListeFichiersSupprimes = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                      Where i.<Repertoire>.Value Like (ChaineLike & "*")
                      Select i
        Dim ListeASupprimer As New List(Of XElement)
        For Each i In ListeFichiersSupprimes
            ListeASupprimer.Add(i)
        Next
        ListeASupprimer.ForEach(Sub(i As XElement)
                                    Dim N As String = i.<Nom>.Value
                                    Dim NomASupprimer As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(N), Path.GetFileName(N))
                                    NotifyShellWatcherFilesDeleted(NomASupprimer)
                                    'i.Remove()
                                End Sub)
    End Sub
        'Traitement du message lors de la notification de renommage d'un repertoire
    Private Sub ShellWatcherFolders_Renamed(ByVal sender As Object, ByVal e As System.IO.RenamedEventArgs) Handles ShellWatcherFolders.Renamed
        ShellUpdateRequest(EnumShellsModifications.RenommerRepertoire, e.FullPath, e.OldFullPath)
    End Sub
    Private Sub NotifyShellWatcherFoldersRenamed(ByVal e As System.IO.RenamedEventArgs)
        Dim RepSup As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(e.OldFullPath), Path.GetFileName(e.OldName))
        Dim RepAjoute As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(e.FullPath), Path.GetFileName(e.Name))
        NotifyShellWatcherFoldersDeleted(RepSup)
        NotifyShellWatcherFoldersCreated(RepAjoute)
        RaiseEvent UpdateDirectoryName(RepAjoute.FullPath, RepSup.FullPath)
    End Sub

    Private Sub ShellUpdateRequest(ByVal Action As EnumShellsModifications, ByVal FullPath As String, ByVal OldFullPath As String)
        _ListShellUpdate.Add(New MemShellsModifications(Action, FullPath, OldFullPath))
        If Not ShellUpdateThreadEnCours Then
            ShellUpdateThreadEnCours = True
            ShellUpdateThread = New Delegate_TacheAsynchrone(AddressOf DelegateShellUpdateRequest)
            ShellUpdateThread.BeginInvoke(Nothing, Nothing)
        End If
    End Sub
    Private Sub DelegateShellUpdateRequest()
        Do
            If MiseAJourOk Then
                Dim cde As MemShellsModifications = _ListShellUpdate.First
                _ListShellUpdate.Remove(_ListShellUpdate.First)
                Dim ARenommer As RenamedEventArgs = New RenamedEventArgs(WatcherChangeTypes.Renamed, Path.GetDirectoryName(cde.FullPath), Path.GetFileName(cde.FullPath), Path.GetFileName(cde.OldFullPath))
                Dim Nouveau As FileSystemEventArgs = New FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(cde.FullPath), Path.GetFileName(cde.FullPath))
                Select Case cde.Action
                    Case EnumShellsModifications.AjouterFichier
                        NotifyShellWatcherFilesCreated(Nouveau)
                    Case EnumShellsModifications.SupprimerFichier
                        NotifyShellWatcherFilesDeleted(Nouveau)
                    Case EnumShellsModifications.ChangerFichier
                        NotifyShellWatcherFilesChanged(Nouveau)
                    Case EnumShellsModifications.RenommerFichier
                        NotifyShellWatcherFilesRenamed(ARenommer)
                    Case EnumShellsModifications.AjouterRepertoire
                        NotifyShellWatcherFoldersCreated(Nouveau)
                    Case EnumShellsModifications.SupprimerRepertoire
                        NotifyShellWatcherFoldersDeleted(Nouveau)
                    Case EnumShellsModifications.RenommerRepertoire
                        NotifyShellWatcherFoldersRenamed(ARenommer)
                End Select
            Else
                Thread.Sleep(100)
            End If
        Loop While _ListShellUpdate.Count > 0
        ShellUpdateThreadEnCours = False
    End Sub

    '**************************************************************************************************************
    '*****************************GESTION DES MENUS CONTEXTUELS LIEES A LA BIBLIOTHEQUE****************************
    '**************************************************************************************************************
    Dim MenuContextuel As New ContextMenu
    Public Function CreationMenuContextuelDynamique(ByVal NomChamp As String, ByVal FonctionReponse As RoutedEventHandler) As ContextMenu
        MenuContextuel = New ContextMenu
        MenuContextuel.Tag = NomChamp
        Dim ListeMenu As New List(Of String) 'Libelle menu;Tag envoyé à la fonction de reponse,Nom sous menu
        Select Case NomChamp
            Case "Recherche"
                ListeMenu.Add("Recherche groupements...;;Groupement")
                ListeMenu.Add("Recherche styles...;;Style")
            Case Else
                Dim ListeElements As IEnumerable(Of String) = SearchListeElements(NomChamp)
                Dim Compteur As Integer = 1
                If ListeElements IsNot Nothing Then
                    For Each i In ListeElements
                        ListeMenu.Add(i & ";" & NomChamp & Compteur & ";")
                        Compteur += 1
                    Next
                End If
        End Select
        ListeMenu.ForEach(Sub(i As String)
                              Dim ItemMenu As New MenuItem
                              Dim TabChaine() As String = Split(i, ";")
                              If TabChaine(0) <> "" Then
                                  If TabChaine(1) <> "" Then
                                      ItemMenu.AddHandler(MenuItem.ClickEvent, FonctionReponse)
                                      ItemMenu.Name = TabChaine(1)
                                      ItemMenu.Tag = TabChaine(1)
                                  End If
                                  If TabChaine(2) <> "" Then CreationItemsDynamiques(TabChaine(2), ItemMenu.Items, FonctionReponse)
                                  ItemMenu.Header = TabChaine(0)
                                  ' ItemMenu.HorizontalAlignment = HorizontalAlignment.Left
                                  MenuContextuel.Items.Add(ItemMenu)
                              Else
                                  MenuContextuel.Items.Add(New Separator)
                              End If
                          End Sub)
        Return MenuContextuel
    End Function
    Private Sub CreationItemsDynamiques(ByVal NomChamp As String, ByVal ItemsMenu As ItemCollection, ByVal FonctionReponse As RoutedEventHandler)
        Select Case NomChamp
            Case Else
                Dim ListeElements As IEnumerable(Of String) = SearchListeElements(NomChamp)
                Dim Compteur As Integer = 1
                If ListeElements IsNot Nothing Then
                    For Each i In ListeElements
                        Dim ItemMenu As New MenuItem
                        ItemMenu.Header = i
                        'ItemMenu.HorizontalAlignment = HorizontalAlignment.Left
                        ItemMenu.AddHandler(MenuItem.ClickEvent, FonctionReponse)
                        ItemMenu.Name = NomChamp & Compteur
                        ItemMenu.Tag = NomChamp & Compteur
                        ItemsMenu.Add(ItemMenu)
                        Compteur += 1
                    Next
                End If
        End Select
        Return
    End Sub
    Private Function SearchListeElements(ByVal NomChamp As String) As IEnumerable(Of String)
        If Not MiseAjourEnCours And (ListeFichiers IsNot Nothing) Then
            SearchEnCours = True
            Dim Resultat As IEnumerable(Of String) = Nothing
            Select Case NomChamp
                Case "Groupement"
                    Resultat = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                                Order By i.<Groupement>.Value
                                Select i.<Groupement>.Value
                                Distinct
                Case "Style"
                    Resultat = From i In ListeFichiers.<Bibliotheque>.<Fichier>
                           Order By i.<Style>.Value
                           Select i.<Style>.Value
                           Distinct
            End Select
            SearchEnCours = False
            Return Resultat
        Else
            Return Nothing
        End If
    End Function


End Class

