Attribute VB_Name = "Macros"
#Const GBDEBUG = False

Private EtatActivationMacros As Boolean
Private MemVisible As Boolean
Private ListingTop As Long
Private ListingLeft As Long
Private BlocageListing As Boolean
Private ImpressionEnCours As Boolean
Private ZoneCopieInterne As Range
Private RepertoireDeBase As String

Public Sub InitialisationMacros(Activer As Boolean) 'Activation et desactivation de la feuille
On Error Resume Next
    Dim zone As Range
#If GBDEBUG Then
    If ValidationDVS And Activer Then 'And MacroInstallationValide Then 'And Not ImpressionEnCours Then
        MacroSurPosteMaitre = True
#Else
    If ValidationDVS And Activer And MacroInstallationValide Then 'And Not ImpressionEnCours Then
#End If
        If Not EtatActivationMacros Then
                If ActiveSheet.Cells(1, 1).Value = "N°" Then
                BlocageModif = True
                DevisEnCours.ProtegerFeuille (False)
                ActiveSheet.Cells(1, 1).Value = "N °"
                DevisEnCours.ProtegerFeuille (True)
                BlocageModif = False
            End If
            ExtractionMaJ = False
            If TestCelluleDansZone(Selection, Range("LISTEEXTRACTIONCOEFS")) Then ZoneExtractionSelectionne = True
            EtatActivationMacros = True
           ' OptionCellDragAndDrop = Application.CellDragAndDrop
           ' Application.CellDragAndDrop = False
            Application.OnKey "+^x", "CouperZone"
            Application.OnKey "+^c", "CopierZone"
            Application.OnKey "+^v", "CollerZone"
            Application.OnKey "^x", "CopierValeur"
            Application.OnKey "^c", "CopierValeur"
            Application.OnKey "^v", "CollerValeur"
            Application.OnKey "^l", "MasquerLigne"
            Application.OnKey "^d", "DecalageDroite"
            Application.OnKey "+^d", "DecalageGauche"
'            Application.OnKey "+^g", "SelectSuivant"
            Application.OnKey "+^l", "MasquerToutesZones"
            Application.OnKey "{F1}", "InsererChapitre"
            Application.OnKey "{F2}", "InsererSousChapitre"
            Application.OnKey "{F3}", "InsererArmoire"
            Application.OnKey "{F4}", "InsererLigne"
'            Application.OnKey "+{F4}", "AjouterLigne"
            Application.OnKey "^{F4}", "ConvertirEnsembleVersElement"
            Application.OnKey "{F6}", "InsererEnsGroupe"
            Application.OnKey "^{F6}", "ConvertirElementVersEnsemble"
            Application.OnKey "{F8}", "InsererEnsCompose"
            Application.OnKey "{F9}", "ConvertirVersPv"
            Application.OnKey "{F10}", "SupprimerZone"
            If MacroSurPosteMaitre Then
                Application.OnKey "+^e", "ExporterXMLFichier"
                Application.OnKey "^e", "ExporterXML"
                Application.OnKey "+^i", "ImporterXMLFichier"
                Application.OnKey "^i", "ImporterXML"
                Application.OnKey "{F5}", "MiseAJourExtraction"
                Application.OnKey "{F11}", "AffichageDialogue"
                Application.OnKey "{F12}", "MiseAJourBDL"
                Application.OnKey "^{ }", "ValidationMiseAJour"
                Application.OnKey "^g", "RegrouperLibelles"
                Application.OnKey "+^g", "RegrouperLibellesListe"
                Application.OnKey "^r", "RemonteLigne"
                Application.OnKey "+^r", ""
                Application.OnKey "+^t", "SupprimerTiret"
            Else
                Application.OnKey "{F11}", ""
                Application.OnKey "+^{E}", "MiseEnFormeEns"
            End If
'            Application.OnKey "{RETURN}", "AppelBD"
'            Application.OnKey "{ENTREE}", "AppelBD"
'            Application.OnKey "{DOWN}", "AppelBDDown"
'            Application.OnKey "^{RETURN}", "AppelBDCTRL"
'            Application.OnKey "^{ENTREE}", "AppelBDCTRL"
            Application.OnKey "+^{U}", "MiseEnFormeUnite"
            Application.OnKey "+^{M}", "MiseEnFormeMl"
            Application.OnKey "+^{²}", "MiseEnFormeM2"
            Application.OnKey "+^{K}", "MiseEnFormeKg"
            Application.OnKey "+^{ }", "MiseEnFormeNull"
            Application.OnKey "+^{P}", "MiseEnFormePM"
        End If
'        Set COLONNEORIGINE = Nothing
'// Version 2.1
        Set ListeColonnes = New Collection
'// END Version 2.1
        Set ColonnePVT2 = Nothing
        For Each zone In ActiveSheet.Range("A1").EntireRow.Cells
            Select Case zone.Value
                Case "N °"
                    Set ColonneCode = zone.EntireColumn
                Case "QTE"
                    Set ColonneQuantite = zone.EntireColumn
                Case "LIBELLE"
                    Set ColonneLibelle = zone.EntireColumn
                Case "MO u"
                    Set ColonneMOU = zone.EntireColumn
                Case "FO u"
                    Set ColonneFOU = zone.EntireColumn
                Case "MO t"
                    Set ColonneMOT = zone.EntireColumn
                Case "FO t"
                    Set ColonneFOT = zone.EntireColumn
                Case "PV u"
                    Set ColonnePVU = zone.EntireColumn
                Case "PV total"
                    Set ColonnePVT = zone.EntireColumn
                Case "Famille"
                    Set ColonneFamille = zone.EntireColumn
       '         Case "Equipement"
       '             Set COLONNEEQUIPEMENT = Zone.EntireColumn
       '             getcolonne(lc_equipement)
       '         Case "Decoupage"
       '             Set COLONNEDECOUPAGE = Zone.EntireColumn
       '         GetColonne (LC_EXTRACTION)
       '         Case "Extraction"
       '             Set COLONNEEXTRACTION = Zone.EntireColumn
       '         Case "Origine"
       '             Set COLONNEORIGINE = Zone.EntireColumn
'//VERSION 1.2
                Case "U"
                    Set ColonneUnite = zone.EntireColumn
'END VERSION 1.2//
'// Version 2.55
                Case "Pv Total 2"
                    Set ColonnePVT2 = zone.EntireColumn
                Case Else
'// Version 2.1
                    If zone.Value <> "" Then
                        ListeColonnes.Add zone.EntireColumn, zone.Value
                    Else
                        Exit For
                    End If
'// END Version 2.1
            End Select
        Next zone
        ColonnesPR = Application.Union(ColonneMOU, ColonneFOU, ColonneMOT, ColonneFOT).AddressLocal
        ColonnesPRZoneArmoire = Application.Union(ColonneCode, ColonneQuantite, ColonneLibelle, ColonneMOU, ColonneFOU, ColonneMOT).AddressLocal
        ColonnesPRZoneAutre = Application.Union(ColonneCode, ColonneQuantite, ColonneLibelle, ColonneMOU, ColonneFOU).AddressLocal
        ColonnesPRLibelleQuantite = Application.Union(ColonneQuantite, ColonneLibelle).AddressLocal
        AjouterMenu
'        Set DevisEnCours.ZoneExportee = ZoneExporteeGlobal
'        If AppExcel Is Nothing Then Set AppExcel = CreateObject("Excel.Application")
'        If ClasseurBD Is Nothing Then Set ClasseurBD = AppExcel.Workbooks.Open(CheminBD & NomBaseDonnees, 0, True)
'        If OngletBD Is Nothing Then Set OngletBD = ClasseurBD.Sheets(NomOngletBD)
        If Not ImpressionEnCours Then BlocageModif = False
    Else
        If EtatActivationMacros Then
            EtatActivationMacros = False
        '    Application.CellDragAndDrop = OptionCellDragAndDrop
            Application.OnKey "+^x"
            Application.OnKey "+^c"
            Application.OnKey "+^v"
            Application.OnKey "^x"
            Application.OnKey "^c"
            Application.OnKey "^v"
            Application.OnKey "^l"
            Application.OnKey "^d"
            If MacroSurPosteMaitre Then
                Application.OnKey "^e", "ExporterDevisv15"
            Else
                Application.OnKey "^e"
            End If
            Application.OnKey "+^e"
            Application.OnKey "+^i"
            Application.OnKey "^i"
            Application.OnKey "+^d"
'            Application.OnKey "+^g"
            Application.OnKey "+^l"
            Application.OnKey "{F1}"
            Application.OnKey "{F2}"
            Application.OnKey "{F3}"
            Application.OnKey "{F4}"
            Application.OnKey "+{F4}"
            Application.OnKey "^{F4}"
            Application.OnKey "{F5}"
            Application.OnKey "{F6}"
            Application.OnKey "^{F6}"
            Application.OnKey "{F8}"
            Application.OnKey "{F9}"
            Application.OnKey "{F10}"
            Application.OnKey "{F11}"
            Application.OnKey "{F12}"
            Application.OnKey "^{ }"
            Application.OnKey "^g"
            Application.OnKey "+^g"
            Application.OnKey "^r"
            Application.OnKey "+^r"
            Application.OnKey "+^t"
'            Application.OnKey "{RETURN}"
'            Application.OnKey "{ENTREE}"
'            Application.OnKey "{DOWN}"
'            Application.OnKey "^{RETURN}"
'            Application.OnKey "^{ENTREE}"
'            Application.OnKey "^{DOWN}"
            Application.OnKey "+^{E}"
            Application.OnKey "+^{U}"
            Application.OnKey "+^{M}"
            Application.OnKey "+^{²}"
            Application.OnKey "+^{K}"
            Application.OnKey "+^{ }"
            Application.OnKey "+^{P}"
        End If
        SupprimerMenu
'        Set COLONNEDECOUPAGE = Nothing
'        Set COLONNEEQUIPEMENT = Nothing
'        Set COLONNEEXTRACTION = Nothing
'        Set COLONNEORIGINE = Nothing
'        Set ColonnesLibres = Nothing
        Set ZoneCopieInterne = Nothing
'// Version 2.1
        Set ListeColonnes = Nothing
'// END Version 2.1
'        If Not DevisEnCours Is Nothing Then Set ZoneExporteeGlobal = DevisEnCours.ZoneExportee
    End If
End Sub

Private Sub SelectSuivant()
'    Dim NouvelleZone As Collection
'    Set NouvelleZone = DevisEnCours.SelectionneZoneMultiple
'    If Not NouvelleZone Is Nothing Then NouvelleZone.Copy
End Sub



Private Sub MiseEnFormeEns()
    Selection.NumberFormat = "### ###" & """ens"""
End Sub
Private Sub MiseEnFormeUnite()
    Selection.NumberFormat = "### ###" & """u"""
End Sub
Private Sub MiseEnFormeMl()
    Selection.NumberFormat = "### ###" & """ml"""
End Sub
Private Sub MiseEnFormeM2()
    Selection.NumberFormat = "### ###" & """m²"""
End Sub
Private Sub MiseEnFormeKg()
    Selection.NumberFormat = "### ###" & """kg"""
End Sub
Private Sub MiseEnFormeNull()
    Selection.NumberFormat = "### ###"
End Sub
Private Sub MiseEnFormePM()
    Selection.NumberFormat = """PM"""
End Sub
Public Sub MiseAJourOnglet(Onglet As Worksheet)
End Sub

Public Sub MiseAJourSelection(Element As Range) 'La selection a changée
    Static ExPos As Integer
'    If Not BlocageModif Or Not BlocageListing Then
'        If DevisEnCours.ArticleEnCours.TypeObjet = "VIDE" Then
'            Application.Selection.Offset(1, 0).Select
'        Else
        If Not BlocageListing Then
            On Error Resume Next
            If Selection.EntireRow.RowHeight >= 0.75 Then
                Select Case DevisEnCours.StylePremiereLigne(Selection)
                    Case "MoTEnsembleMasque", "MOTChapitreMasque", "MOTSousChapitreMasque"
                        Selection.EntireRow.RowHeight = 0
                    Case "FinCompose", "FinEnsemble"
                        Selection.EntireRow.RowHeight = 0.75
                End Select
            End If
            If TestCelluleDansZone(Element, Range("LISTEEXTRACTIONCOEFS")) Then
                If (Not ExtractionMaJ) And (Not ZoneExtractionSelectionne) Then
                    ExtractFamilles
                    ZoneExtractionSelectionne = True
                    ExtractionMaJ = True
                End If
            Else
                ZoneExtractionSelectionne = False
            End If
            If Selection.EntireRow.RowHeight < 1 Then
                If ExPos < Selection.Row Then
                    Selection.Offset(1, 0).Select
                Else
                    Selection.Offset(-1, 0).Select
                End If
            Else
                Call AfficherDialogue(Element, False)
            End If
            ExPos = Selection.Row
        End If
'        End If
'    End If
End Sub
Public Function BeforeDoubleClick(Element As Range, Cancel As Boolean) As Boolean 'Double click de la souris
    Dim Tabposition As POINTAPI
On Error GoTo fin:
    If ValidationDVS Then
        If Element.EntireColumn.Cells.Count = Element.Count Then 'Selection d'une colonne complete
        End If
        If Element.EntireRow.Cells.Count = Element.Count Then 'Selection d'une ligne complete
        End If
        If Element.Cells.Count = 1 And MacroSurPosteMaitre Then
            Call DevisEnCours.MiseAJourEmplacement(Selection)
            Select Case DevisEnCours.ArticleEnCours.TypeObjet()
                Case "SOUSCHAPITRE", "CHAPITRE", "ENSCOMPOSE", "ENSGROUPE"
                    MasquerLigne
                Case Else
                    If TestCelluleDansZone(Element, ColonneCode) And Element.Locked = False Then
                        Load SaisieCodeLocal
                        SaisieCodeLocal.StartUpPosition = 2
                        SaisieCodeLocal.Show 0
                        BeforeDoubleClick = True
                    End If

            End Select
        End If
    End If
    Exit Function
fin:
    BeforeDoubleClick = False
End Function
Public Function BeforeRightClick(Element As Range, Cancel As Boolean) As Boolean 'Click droit sur la souris
On Error GoTo fin:
    If ValidationDVS Then
        If Element.EntireColumn.Cells.Count = Element.Count Then 'Selection d'une colonne complete
             BeforeRightClick = True 'annulation menu colonne sur les colonnes protégées
        End If
        If Element.EntireRow.Cells.Count = Element.Count Then 'Selection d'une ligne complete
            'BeforeRightClick = True 'Annulation du menu par défaut sur toute la ligne
        End If
        If Element.Cells.Count = 1 And MacroSurPosteMaitre Then
            Call DevisEnCours.MiseAJourEmplacement(Selection)
            Select Case DevisEnCours.ArticleEnCours.TypeObjet()
                Case "LIGNESOUSCHAPITRE", "LIGNECHAPITRE", "ENSCOMPOSE", "ENSGROUPE", "LIGNEENSCOMPOSE", "LIGNEENSGROUPE"
                    If GetColonne(LC_ORIGINE) Is Nothing Then
                        NomListeEquipements = GetValeurNom("NOMONGLETLISTEEQUIPEMENTS")
                        If NomListeEquipements = "" Then
                            NomListeEquipements = GetNomOnglet("DVSREPERECOMP", "ONGLETLISTEEQUIPEMENTS")
                        End If
                    Else
                        NomListeEquipements = Application.Intersect(Element.EntireRow, GetColonne(LC_ORIGINE)).Value
                        If NomListeEquipements = "" Then
                            NomListeEquipements = GetNomOnglet("DVSREPERECOMP", "ONGLETLISTEEQUIPEMENTS")
                        End If
                    End If
                    If TestCelluleDansZone(Element, GetColonne(LC_EQUIPEMENT)) Then
                        Retour = MenuContextuelVariable(Worksheets(NomListeEquipements).Range("ListeCategoriesEquipements"), Worksheets(NomListeEquipements).Range("ListeNomsEquipements"), True, False, False)
                        If Retour <> 0 Then Element.Value = Retour ' "=" & Retour
                        BeforeRightClick = True
                    ElseIf TestCelluleDansZone(Element, GetColonne(LC_DECOUPAGE)) Then
                        Retour = MenuContextuelVariable(Worksheets(NomListeEquipements).Range("ListeDecoupage"), Worksheets(NomListeEquipements).Range("ListeDecoupageAb"), False, True, False)
                        If Retour <> 0 Then Element.Value = Retour '"=" & Retour
                        BeforeRightClick = True
                    ElseIf TestCelluleDansZone(Element, GetColonne(LC_ORIGINE)) Then
                        Retour = AffichageMenuListeDVSComp
                        If Retour <> 0 Then Element.Value = Retour
                        BeforeRightClick = True
                    Else
                        Select Case DevisEnCours.ArticleEnCours.TypeObjet()
                            Case "LIGNESOUSCHAPITRE", "LIGNECHAPITRE"
                                If TestCelluleDansZone(Selection, ColonneQuantite) Then
        '                            BeforeRightClick = True 'Annule l'action par défaut
                                End If
                        End Select
                    End If
             End Select
            'End If
        End If
    End If
fin:
End Function
Public Sub CelluleChange(Element As Range) 'Modification du contenu d'une cellule
Attribute CelluleChange.VB_ProcData.VB_Invoke_Func = "W\n14"
    Dim MemPosition As Range
    Dim a As Worksheet
On Error GoTo fin
    DevisEnCours.CouperZoneAnnuler
    If Not TestCelluleDansZoneNom(Element, "LISTEEXTRACTIONCOEFS") Then ExtractionMaJ = False
    If Not BlocageModif Then
        If ValidationDVS Then
            Application.Interactive = False
            Debug.Print Application.Interactive & Now()
            If Not ActiveSheet.ProtectContents Then
                If ActiveSheet.Names("XXX_MODIFDEVISHORSPROG").Value = "=FALSE" Then
                    ActiveSheet.Names("XXX_MODIFDEVISHORSPROG").Value = True
                End If
            End If
            Set MemPosition = Application.Selection
            If TestCelluleDansZone(Element, ColonneCode) And Element.Style.Name = "NChapitre" Then
'                BlocageListing = True
'                BlocageModif = True
'                Application.Interactive = False
'                Application.Calculation = xlManual
'                Call DevisEnCours.ReformatSelection(MemPosition, False)
'                Call DevisEnCours.MiseAJourEmplacement(Element)
'                For Each i In DevisEnCours.ArticleEnCours.TrouveElements()
'                    i.Value
'                Next i
'                If MemPosition.Address <> Element.Address Then
'                    MemPosition.Select
'                End If
'                Application.Interactive = True
'                BlocageModif = False
'                BlocageListing = False
            Else
                If TestCelluleDansZone(Element, ColonneCode) And Element.Style.Name <> "LigneRecap" Then
                    BlocageListing = True
                    BlocageModif = True
                    Application.Interactive = False
                    Application.Calculation = xlManual
                    Call DevisEnCours.ReformatSelection(MemPosition, False)
                    Call DevisEnCours.AppelBD(Element, False)
                    If MemPosition.Address <> Element.Address Then
                        MemPosition.Select
                    End If
                    Application.Interactive = True
                    Debug.Print Application.Interactive & Now()
                    BlocageModif = False
                    BlocageListing = False
                End If
            End If
            If TestCelluleDansZone(Element, ColonneQuantite) And Element.Style.Name <> "LigneRecap" And MacroSurPosteMaitre Then
                Dim ExStyle As String
                ExStyle = Element.Style
                If EstFormule(Element) Then
                    With Element.Interior
                        .ColorIndex = 20
                        .Pattern = xlSolid
                    End With
                    Element.Font.ColorIndex = 3
                Else
                    Element.Style = ExStyle
                End If
            End If
            If TestCelluleDansZone(Element, GetColonne(LC_EXTRACTION)) Or TestCelluleDansZone(Element, GetColonne(LC_DECOUPAGE)) Or TestCelluleDansZone(Element, GetColonne(LC_EQUIPEMENT)) Or TestCelluleDansZone(Element, GetColonne(LC_ORIGINE)) And MacroSurPosteMaitre Then
                BlocageListing = True
                BlocageModif = True
                Call DevisEnCours.ReformatSelection(MemPosition, False)
                Call DevisEnCours.AppelExtraction(Element)
                If MemPosition.Address <> Element.Address Then
                    MemPosition.Select
                End If
                BlocageModif = False
                BlocageListing = False
            End If
            Application.Calculation = xlAutomatic
            Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        End If
    End If
Exit Sub
fin:
    Application.Interactive = True
    Debug.Print Application.Interactive & Now()
End Sub
Private Function ValidationDVS() As Boolean
    Dim DevisOk As Boolean
    Dim TrameVersion As String
On Error GoTo fin
    If ActiveWindow.SelectedSheets.Count > 1 Then Exit Function
    If Not ImpressionEnCours And NomExiste("XXX_MODIFDEVISHORSPROG", ActiveSheet) Then
        If Not ActiveWorkbook.ActiveChart Is Nothing Then Exit Function
        If ActiveSheet.Names("XXX_MODIFDEVISHORSPROG").Value = "=TRUE" Then
            If Not MemAffichageAlerte Then MsgBox "Impossible d'utiliser le programme sur un devis modifié sans protection.", vbExclamation
            MemAffichageAlerte = True
            ActiveSheet.Activate
            Exit Function
        Else
            MemAffichageAlerte = False
        End If
    Else
        MemAffichageAlerte = False
    End If
    If NomExiste("XXX_DEVIS", ActiveSheet) And OngletExiste("Trames", ActiveWorkbook) Then
        Select Case ActiveSheet.Names("XXX_DEVIS").RefersTo
            Case "=1"
                If ClasseurVersion = "" Then
                    DevisPassWord = ""
                    DevisVersion = "1"
                    DevisOk = True
                End If
            Case "=1.1"                 'Modification majeur de la selection des zones
                If Not NomExiste("XXX_VERSION", ActiveWorkbook.Worksheets("Trames")) Then
                    If Not MemConnexionPrecedente = "=1.1" Then
                        If MsgBox("Le classeur doit être modifié pour fonctionner avec la nouvelle version du programme. Confirmer la modification du classeur ?", vbQuestion + vbYesNo) = vbYes Then
                            ActiveWorkbook.Worksheets("Trames").Names.Add "XXX_VERSION", "=1.1", False
                        Else
                            MemConnexionPrecedente = "=1.1"
                            Exit Function
                        End If
                    Else
                        Exit Function
                    End If
                End If
                TrameVersion = ActiveWorkbook.Worksheets("Trames").Names("XXX_VERSION").RefersTo
                If TrameVersion = "=1.1" Then
                    DevisPassWord = "GFC"
                    DevisVersion = "1.1"
                    DevisOk = True
                    MemConnexionPrecedente = ""
                Else
                    If MemConnexionPrecedente <> "=1.1" Then
                        MemConnexionPrecedente = "=1.1"
                        MsgBox "Onglet devis non compatible avec la version du classeur", vbExclamation
                    End If
                End If
    '//VERSION 1.2
            Case "=1.2"                 'Introduction de la colonne U
                TrameVersion = ActiveWorkbook.Worksheets("Trames").Names("XXX_VERSION").RefersTo
                If TrameVersion = "=1.2" Then
                    DevisPassWord = "GFC"
                    DevisVersion = "1.2"
                    DevisOk = True
                    MemConnexionPrecedente = ""
                Else
                    If MemConnexionPrecedente <> "=1.2" Then
                        MemConnexionPrecedente = "=1.2"
                        MsgBox "Onglet devis non compatible avec la version du classeur", vbExclamation
                    End If
                End If
    'END VERSION 1.2//
    '//VERSION 2
            Case "=2", "=2.1", "=2.15", "=2.2", "=2.3", "=2.4", "=2.5", "=2.55"       'Modification des trames pour nouvelle mise en forme
                TrameVersion = ActiveWorkbook.Worksheets("Trames").Names("XXX_VERSION").RefersTo
                If TrameVersion = ActiveSheet.Names("XXX_DEVIS").RefersTo Then
                    DevisPassWord = "GFC"
                    DevisVersion = ExtraitChaine(ActiveSheet.Names("XXX_DEVIS").RefersTo, "=", "") '"2"
                    DevisOk = True
                    MemConnexionPrecedente = ""
                Else
                    If MemConnexionPrecedente <> ActiveSheet.Names("XXX_DEVIS").RefersTo Then
                        MemConnexionPrecedente = ActiveSheet.Names("XXX_DEVIS").RefersTo
                        MsgBox "Onglet devis non compatible avec la version du classeur", vbExclamation
                    End If
                End If
    'END VERSION 2//
        End Select
        If DevisOk Then
            If DevisEnCours Is Nothing Then Set DevisEnCours = New Devis
            If Not BlocageListing Then DevisEnCours.OngletActive
            ValidationDVS = True
        End If
    End If
fin:
End Function
Private Sub SupprimerTiret()
Dim Cellule As Range
On Error Resume Next
    If ValidationDVS Then
        BlocageListing = True
        BlocageModif = True
        Application.Interactive = False
'        Set Cellule = Selection.Cells(1, 1)
        For Each i In Selection.Cells
            If Left(i.Value, 1) = "-" Then i.Value = Trim(Right(i.Value, Len(i.Value) - 1))
        Next i
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        BlocageModif = False
        BlocageListing = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub RegrouperLibelles()
Dim Cellule As Range
Dim CumulerLibelle As Boolean
On Error Resume Next
    If ValidationDVS Then
        BlocageListing = True
        BlocageModif = True
        Application.Interactive = False
        Set Cellule = Selection.Cells(1, 1)
        For Each i In Selection.Cells
            If CumulerLibelle Then Cellule.Value = Trim(Cellule.Value) & " " & Trim(i.Value)
            CumulerLibelle = True
        Next i
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        BlocageModif = False
        BlocageListing = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub RegrouperLibellesListe()
Dim Cellule As Range
Dim CumulerLibelle As Boolean
On Error Resume Next
    If ValidationDVS Then
        BlocageListing = True
        BlocageModif = True
        Application.Interactive = False
        Set Cellule = Selection.Cells(1, 1)
        For Each i In Selection.Cells
            If CumulerLibelle Then Cellule.Value = Cellule.Value & Chr(10) & i.Value
            CumulerLibelle = True
        Next i
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        BlocageModif = False
        BlocageListing = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub ExtractFamilles()
On Error Resume Next
    If ValidationDVS And (val(DevisVersion) >= 2.4) Then
        BlocageListing = True
        BlocageModif = True
        Application.Calculation = xlManual
        Application.Interactive = False
        Call DevisEnCours.ExtractFamilles
        Application.Calculation = xlAutomatic
       '     Debug.Print Application.Interactive & Now()
        Application.Interactive = True
        BlocageModif = False
        BlocageListing = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub ExporterXML()
On Error Resume Next
    If ValidationDVS Then
        BlocageListing = True
        BlocageModif = True
        Application.Interactive = False
        Call DevisEnCours.ExportXML 'PRZone
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        BlocageModif = False
        BlocageListing = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub ExporterXMLFichier()
'On Error Resume Next
    Dim fd As FileDialog
    If ValidationDVS Then
        If RepertoireDeBase = "" Then RepertoireDeBase = RepertoireData & "\Modeles\"
        BlocageListing = True
        BlocageModif = True
        Application.Interactive = False
        Set fd = Application.FileDialog(msoFileDialogSaveAs)
        With fd
            .InitialFileName = RepertoireDeBase & "Export.xml"
            .ButtonName = "Exporter"
            .Title = "Nom du fichier d'exportation"
            For i = 1 To .Filters.Count
                If .Filters.Item(i).Extensions = "*.xml" Then
                    .FilterIndex = i
                    Exit For
                End If
            Next i
            If .Show = -1 Then
                RepertoireDeBase = GetFilePath(.SelectedItems(1)) & "\"
                If .Filters.Item(.FilterIndex).Extensions <> "*.xml" Then
                    MsgBox "Echec de l'exportation. L'extension du fichier doit être du type """ & "*.xml" & """"
                Else
                    Call DevisEnCours.ExportXML(.SelectedItems(1))
                End If
            End If
        End With
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        BlocageModif = False
        BlocageListing = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub ImporterXMLFichier()
    Dim fd As FileDialog
On Error Resume Next
    If ValidationDVS Then
        If RepertoireDeBase = "" Then RepertoireDeBase = RepertoireData & "\Modeles\"
        BlocageListing = True
        BlocageModif = True
        Application.Calculation = xlManual
        Application.Interactive = False
        
        Set fd = Application.FileDialog(msoFileDialogOpen)
        With fd
            .ButtonName = "Importer"
            .Title = "Nom du fichier à importer"
            .InitialFileName = ""
            For i = 1 To .Filters.Count
                If .Filters.Item(i).Extensions = "*.xml" Then
                    .FilterIndex = i
                    Exit For
                End If
            Next i
            If .Show = -1 Then
                RepertoireDeBase = GetFilePath(.SelectedItems(1)) & "\"
                If .Filters.Item(.FilterIndex).Extensions <> "*.xml" Then
                    MsgBox "Echec de l'importation. L'extension du fichier doit être du type """ & "*.xml" & """"
                Else
                    Call DevisEnCours.ImportXML(.SelectedItems(1))
                End If
            End If
        End With
        
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        Application.Calculation = xlAutomatic
        BlocageModif = False
        BlocageListing = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub ImporterXML()
On Error Resume Next
    If ValidationDVS Then
        BlocageListing = True
        BlocageModif = True
        Application.Calculation = xlManual
        Application.Interactive = False
        Call DevisEnCours.ImportXML 'PRZone
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        Application.Calculation = xlAutomatic
        BlocageModif = False
        BlocageListing = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub RemonteLigne()
On Error Resume Next
    If ValidationDVS Then
        BlocageListing = True
        BlocageModif = True
'        Application.Calculation = xlManual
        Application.Interactive = False
        Call DevisEnCours.RemonteLigne   'PRZone
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
'        Application.Calculation = xlAutomatic
        BlocageModif = False
        BlocageListing = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub CopierZone()
On Error Resume Next
    If ValidationDVS Then
        BlocageListing = True
        BlocageModif = True
'        Application.Calculation = xlManual
        Application.Interactive = False
        Call DevisEnCours.CopierZone(False) 'PRZone
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
'        Application.Calculation = xlAutomatic
        BlocageModif = False
        BlocageListing = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub CouperZone()
On Error Resume Next
    If ValidationDVS Then
        BlocageListing = True
        BlocageModif = True
'        Application.Calculation = xlManual
        Application.Interactive = False
        Call DevisEnCours.CopierZone(True)    'PRZone
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
'        Application.Calculation = xlAutomatic
        BlocageModif = False
        BlocageListing = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub CollerZone()
On Error Resume Next
    If ValidationDVS Then
        BlocageListing = True
        BlocageModif = True
        Application.Calculation = xlManual
        Application.Interactive = False
        Application.StatusBar = "Opération en cours..."
        DevisEnCours.CollerZone 'PRZone
        Application.Interactive = True
             Debug.Print Application.Interactive & Now()
       Application.Calculation = xlAutomatic
        Application.StatusBar = False
        BlocageModif = False
        BlocageListing = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub DecalageDroite()
    On Error Resume Next
    If ValidationDVS Then
        BlocageModif = True
        DevisEnCours.ProtegerFeuille (False)
        Selection.InsertIndent 1
        DevisEnCours.ProtegerFeuille (True)
        BlocageModif = False
    End If
End Sub
Private Sub DecalageGauche()
    On Error Resume Next
    If ValidationDVS Then
        BlocageModif = True
        DevisEnCours.ProtegerFeuille (False)
        Selection.InsertIndent -1
        DevisEnCours.ProtegerFeuille (True)
        BlocageModif = False
    End If
End Sub
Private Sub CopierValeur()
On Error GoTo fin
    Selection.Copy
    Set ZoneCopieInterne = Selection
    Exit Sub
fin:
'    MsgBox "La cellule est protégé ou en lecture seule", vbExclamation
End Sub
Private Sub CollerValeur()
Dim MemSelect As Range
Dim Action As Long
On Error Resume Next
    Application.ScreenUpdating = False
    If Not ZoneCopieInterne Is Nothing Then
        ZoneCopieInterne.Copy
        Action = xlPasteFormulas
    Else
         Action = xlPasteValues
    End If
    If Selection.EntireColumn.Count = 1 Then
        BlocageListing = True
        BlocageModif = True
        Application.Interactive = False
        Set MemSelect = Selection
        For Each i In Selection.Cells
            If (i.EntireRow.Hidden = False) And i.EntireRow.Height > 3 Then
                i.PasteSpecial Action
            End If
            If i.Row > Selection.SpecialCells(xlCellTypeLastCell).Row Then Exit For
        Next i
        MemSelect.Select
        BlocageModif = False
        If MemSelect.EntireColumn.Count = 1 Then
            If TestCelluleDansZone(MemSelect, GetColonne(LC_DECOUPAGE)) Or _
                        TestCelluleDansZone(MemSelect, GetColonne(LC_EQUIPEMENT)) Or _
                        TestCelluleDansZone(MemSelect, GetColonne(LC_EXTRACTION)) Or _
                        TestCelluleDansZone(MemSelect, GetColonne(LC_ORIGINE)) Then
                If MemSelect.EntireRow.Count > 1 Then
                    Call MiseAJourExtraction
                Else
                    MemSelect.Value = MemSelect.Value
                End If
            ElseIf TestCelluleDansZone(MemSelect, ColonneCode) Then
                For Each i In MemSelect.Cells
                    i.Value = i.Value
                Next i
            End If
        End If
        BlocageListing = False
        Application.Interactive = True
        Debug.Print Application.Interactive & Now()
    Else
        Selection.PasteSpecial Action
    End If
    Application.ScreenUpdating = False
    Exit Sub
'fin:
'    Application.Interactive = True
'            Debug.Print Application.Interactive & Now()
'    BlocageListing = False
'    BlocageModif = False
    '    MsgBox "La cellule est protégé ou en lecture seule", vbExclamation
End Sub
Private Sub InsererLigne()
On Error Resume Next
    If ValidationDVS Then
        BlocageModif = True
        Application.Calculation = xlManual
        Application.Interactive = False
        DevisEnCours.InsererLigne
        Application.Interactive = True
             Debug.Print Application.Interactive & Now()
       Application.Calculation = xlAutomatic
        BlocageModif = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
'Private Sub AjouterLigne()
'    If ValidationDVS Then
'        BlocageModif = True
'        DevisEnCours.SelectionneDerniereLigne
'        DevisEnCours.InsererLigne
'        BlocageModif = False
'        Call AfficherDialogue(Application.Selection, False)
'    End If
'End Sub
Private Sub InsererChapitre()
On Error Resume Next
    Dim Mem As Range
    If ValidationDVS Then
        BlocageModif = True
        Application.Calculation = xlManual
        Application.Interactive = False
        If DevisEnCours.InsererChapitre Then
            Set Mem = Application.Selection
            DevisEnCours.AjouterLigne (10)
            Mem.Select
        End If
        Application.Interactive = True
             Debug.Print Application.Interactive & Now()
       Application.Calculation = xlAutomatic
        BlocageModif = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub InsererSousChapitre()
Attribute InsererSousChapitre.VB_ProcData.VB_Invoke_Func = "s\n14"
On Error Resume Next
    Dim Mem As Range
    If ValidationDVS Then
        BlocageModif = True
        Application.Calculation = xlManual
        Application.Interactive = False
        If DevisEnCours.InsererSousChapitre Then
            Set Mem = Application.Selection
            DevisEnCours.AjouterLigne (6)
            Mem.Select
        End If
        Application.Interactive = True
              Debug.Print Application.Interactive & Now()
      Application.Calculation = xlAutomatic
        BlocageModif = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub InsererEnsGroupe()
Attribute InsererEnsGroupe.VB_ProcData.VB_Invoke_Func = "e\n14"
On Error Resume Next
    Dim Mem As Range
    If ValidationDVS Then
        BlocageModif = True
        Application.Calculation = xlManual
        Application.Interactive = False
        If DevisEnCours.InsererEnsGroupe Then
            Set Mem = Application.Selection
            DevisEnCours.AjouterLigne (2)
            Mem.Select
            'DevisEnCours.ArticleEnCours
        End If
        Application.Interactive = True
             Debug.Print Application.Interactive & Now()
       Application.Calculation = xlAutomatic
        BlocageModif = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub InsererEnsCompose()
Attribute InsererEnsCompose.VB_ProcData.VB_Invoke_Func = "z\n14"
On Error Resume Next
    Dim Mem As Range
    If ValidationDVS Then
        BlocageModif = True
        Application.Calculation = xlManual
        Application.Interactive = False
        If DevisEnCours.InsererEnsCompose Then
            Set Mem = Application.Selection
            DevisEnCours.AjouterLigne (2)
            Mem.Select
        End If
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        Application.Calculation = xlAutomatic
        BlocageModif = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub InsererArmoire()
Attribute InsererArmoire.VB_ProcData.VB_Invoke_Func = "a\n14"
On Error Resume Next
    Dim Mem As Range
    If ValidationDVS Then
        BlocageModif = True
        Application.Calculation = xlManual
        Application.Interactive = False
        If DevisEnCours.InsererArmoire Then
            Set Mem = Application.Selection
            DevisEnCours.AjouterLigne (5)
            Mem.Select
        End If
        Application.Calculation = xlAutomatic
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        BlocageModif = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
'Private Sub InsererEtude()
'    If ValidationDVS Then
'        BlocageModif = True
'        DevisEnCours.InsererEtude
'        BlocageModif = False
'        Call AfficherDialogue(Application.Selection, False)
'    End If
'End Sub
Private Sub SupprimerZone()
Attribute SupprimerZone.VB_ProcData.VB_Invoke_Func = "u\n14"
On Error Resume Next
    Dim zone As Range
    If ValidationDVS Then
        BlocageModif = True
        BlocageListing = True
        Application.Calculation = xlManual
        Application.Interactive = False
        DevisEnCours.SupprimerZone
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        Application.Calculation = xlAutomatic
        BlocageListing = False
        BlocageModif = False
        Call AfficherDialogue(Application.Selection, False)
    End If
End Sub
Private Sub MasquerLigne()
Dim ZoneSelectionnee As Range
    If ValidationDVS Then
        BlocageModif = True
        BlocageListing = True
        Application.ScreenUpdating = False
        Application.Calculation = xlManual
        Application.Interactive = False
        Set ZoneSelectionnee = Selection
        If ZoneSelectionnee.EntireRow.Rows.Count = 1 Then
            Call DevisEnCours.MasquerLigne
        Else
            Debut = ZoneSelectionnee.Row
            fin = ZoneSelectionnee.Row + ZoneSelectionnee.Rows.Count - 1
            For i = Debut To fin
                Set LigneCourante = ActiveSheet.Cells(i, 1)
                LigneCourante.Select
                Call DevisEnCours.MasquerLigne
            Next i
            ZoneSelectionnee.Select
        End If
        Application.Calculation = xlAutomatic
        Application.Interactive = True
        BlocageListing = False
        BlocageModif = False
    End If
End Sub
Private Sub MasquerToutesZones()
On Error Resume Next
    LignesACacher = Null
    If ValidationDVS Then
        Application.Calculation = xlManual
        Application.Interactive = False
        For i = 1 To Range("ENSEMBLE_DEVIS").Rows.Count
            Set LigneCourante = Cells(i, 1)
            If LigneCourante.Interior.ColorIndex = 15 Then
                If IsNull(LignesACacher) Then
                    Set LignesACacher = LigneCourante
                Else
                    Set LignesACacher = Union(LignesACacher, LigneCourante)
                End If
            End If
        Next i
        If Not IsNull(LignesACacher) Then
            If LignesACacher.EntireRow.Height <> 0 Then
                LignesACacher.EntireRow.RowHeight = 0
            Else
                LignesACacher.EntireRow.AutoFit
            End If
        End If
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        Application.Calculation = xlAutomatic
    End If
End Sub
Private Sub MiseAJourBDL()
    Call MiseAJourBD(True)
End Sub
Public Sub MiseAJourBD(Optional Locale As Boolean = False)
Dim Reponse As Long
On Error Resume Next
    If ValidationDVS Then
        If Application.Selection.Rows.Count > 1 Then
            Reponse = vbYes
            Locale = False
        Else
            If Locale Then
                Reponse = MsgBox("Voulez-vous mettre à jour les données locales venant des autres onglets devis ?", vbQuestion + vbYesNo, "Mise à jour devis")
            Else
                Reponse = MsgBox("Voulez-vous mettre à jour l'ensemble des données ?", vbQuestion + vbYesNo, "Mise à jour devis")
            End If
        End If
        If Reponse = vbYes Then
            BlocageModif = True
            BlocageListing = True
            Application.Calculation = xlManual
            Application.Interactive = False
            DevisEnCours.MiseAJourBD Locale
            Application.Interactive = True
              Debug.Print Application.Interactive & Now()
          Application.Calculation = xlAutomatic
            BlocageListing = False
            BlocageModif = False
        End If
    End If
End Sub
Public Sub MiseAJourExtraction()
Dim Retour As Long
On Error Resume Next
    Application.Interactive = False
    If ValidationDVS Then
        If Application.Selection.Rows.Count > 1 Then
            Retour = vbYes
        Else
            Retour = MsgBox("Voulez-vous mettre à jour les données extraites ?", vbQuestion + vbYesNo + vbDefaultButton2, "Mise à jour des données")
        End If
        If Retour = vbYes Then
            BlocageModif = True
            BlocageListing = True
            Application.Calculation = xlManual
         '   Call RAZRechercheValeurListe
            DevisEnCours.MiseAJourExtraction
            Application.Calculation = xlAutomatic
            BlocageListing = False
            BlocageModif = False
        End If
    End If
    Application.Interactive = True
            Debug.Print Application.Interactive & Now()
End Sub
Private Function ValidationMiseAJour() As Boolean
On Error Resume Next
    If ValidationDVS Then
        BlocageModif = True
        BlocageListing = True
        Application.Calculation = xlManual
        Application.Interactive = False
        ValidationMiseAJour = DevisEnCours.ValidationMiseAJour
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        Application.Calculation = xlAutomatic
        BlocageListing = False
        BlocageModif = False
    End If
End Function
Private Sub ConvertirElementVersEnsemble()
On Error Resume Next
    If ValidationDVS Then
        BlocageListing = True
        BlocageModif = True
        Application.Calculation = xlManual
        Application.Interactive = False
        DevisEnCours.ConvertirElementVersEnsemble
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        Application.Calculation = xlAutomatic
        BlocageModif = False
        BlocageListing = False
    End If
End Sub
Private Sub ConvertirEnsembleVersElement()
On Error Resume Next
    If ValidationDVS Then
        BlocageListing = True
        BlocageModif = True
        Application.Calculation = xlManual
        Application.Interactive = False
        Call DevisEnCours.ConvertirEnsembleVersElement(False)
        Application.Interactive = True
            Debug.Print Application.Interactive & Now()
        Application.Calculation = xlAutomatic
        BlocageModif = False
        BlocageListing = False
    End If
End Sub
Private Sub AffichageDialogue()
    Call AfficherDialogue(Selection, True)
End Sub
Private Function AfficherDialogue(Element As Range, Ouverture As Boolean) As Boolean
Attribute AfficherDialogue.VB_ProcData.VB_Invoke_Func = " \n14"
    Dim Liste As Collection
  '  Dim Decalage As Integer
  '  Dim i As Object
  '  Dim NodeEnCours As Node
  '  Dim NodeParent As Node
  '  Dim KeyNodeParent As String
  '  Dim Colonne As Range
  '  Dim LigneSelectionnee As Range
  '  If ValidationDVS And Not Element Is Nothing Then
  '      If (MemVisible Or Listing.Visible Or Ouverture) And Not BlocageListing Then
  '          MemVisible = False
  '
  '          Set Element = Element.Cells(1.1)
  '      '    Listing.ListeElements.Clear
  '          Listing.ListeLignes.Nodes.Clear
  '          'Listing.ListeParents.Clear
  '          Listing.ListeParents2.Nodes.Clear
  '          Set Liste = ListeDesparents(Element)
  '          'Listing.ListeParents.AddItem 1
  '          'Listing.ListeParents.List(Listing.ListeParents.ListCount - 1, 1) = "DEVIS :"
  '          Set NodeParent = Listing.ListeParents2.Nodes.Add(, , "key:01", "DEVIS :")
  '          NodeParent.Expanded = True
  '          KeyNodeParent = "key:01"
  '          For j = Liste.Count To 1 Step -1
  '              typelignedevis = DevisEnCours.CreationObjetLigne(Liste.Item(j)).TypeObjet
  '              If typelignedevis = "CHAPITRE" Or typelignedevis = "SOUSCHAPITRE" Then
  '                  Chaine = ValeurColonne(Liste.Item(j).EntireRow, ColonneCode) & "-"
  '              Else
  '                  Chaine = "+"
  '              End If
  '              'Listing.ListeParents.AddItem Liste.Item(j).Row
  '              'Listing.ListeParents.List(Listing.ListeParents.ListCount - 1, 1) = Chaine & String((Liste.Count - j + 1) * 4, "    ") & ValeurColonne(Liste.Item(j).EntireRow, ColonneLibelle) 'CStr(Liste.Item(j).EntireRow.Cells(1, 2).Value)
  '              Set NodeParent = Listing.ListeParents2.Nodes.Add(KeyNodeParent, tvwChild, "key:" & Liste.Item(j).Row, ValeurColonne(Liste.Item(j).EntireRow, ColonneLibelle))
  '              NodeParent.Expanded = True
  '              KeyNodeParent = "key:" & Liste.Item(j).Row
  '          Next j
  '          If Element.Row <> 1 Then
   '             Set NodeParent = Listing.ListeParents2.Nodes.Add(KeyNodeParent, tvwChild, "key:" & Element.Row, ValeurColonne(Element.EntireRow, ColonneLibelle))
   '             NodeParent.Expanded = True
   '             KeyNodeParent = "key:" & Element.Row
   '         End If
   '         'Listing.ListeParents.AddItem ActiveSheet.Range("MODELELIGNERECAP").Row
   '         'Listing.ListeParents.List(Listing.ListeParents.ListCount - 1, 1) = "RECAPITULATIF :"
   '         Set NodeParent = Listing.ListeParents2.Nodes.Add(, , "key:" & ActiveSheet.Range("MODELELIGNERECAP").Row, "RECAPITULATIF :")
   '
   '         Set Liste = ListerElements(Element)
   '         If Not Liste Is Nothing Then
   '             Call DevisEnCours.ReformatSelection(Element)
   '             Chaine = ValeurColonne(Element.EntireRow, ColonneLibelle) 'CStr(Element.EntireRow.Cells(1, 2).Value) '& Chr(13)
'  '              Listing.Caption = Chaine
   '             'Listing.LibelleLigneSelect.Caption = Chaine
   '             'INSERER TOUTES LES INFORMATIONS DE LA LIGNE
   '             'xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   '             Listing.Decoupage.Text = ""
   '             Listing.Equipement.Text = ""
   '             Listing.Extraction.Text = ""
   '             Listing.Origine.Text = ""
   '             Listing.Code.Visible = True
   '             Listing.Decoupage.Visible = True
   '             Listing.Equipement.Visible = True
   '             Listing.Extraction.Visible = True
   '             Listing.Origine.Visible = True
   '             Listing.Repere.Text = ""
   '             typelignedevis = DevisEnCours.CreationObjetLigne(Element).TypeObjet
   '             If Element.Row <> 1 And typelignedevis <> "VIDE" And typelignedevis <> "CHAPITRE" And typelignedevis <> "SOUSCHAPITRE" Then
   '                 Set Colonne = ColonneCode
   '                 If Not Colonne Is Nothing Then Listing.Code.Text = ValeurColonne(Element.EntireRow, Colonne)
  '                  Set Colonne = GetColonne(LC_DECOUPAGE)
   '                 If Not Colonne Is Nothing Then Listing.Decoupage.Text = ValeurColonne(Element.EntireRow, Colonne)
   '                 Set Colonne = GetColonne(LC_EQUIPEMENT)
   '                 If Not Colonne Is Nothing Then Listing.Equipement.Text = ValeurColonne(Element.EntireRow, Colonne)
   '                 Set Colonne = GetColonne(LC_EXTRACTION)
   '                 If Not Colonne Is Nothing Then Listing.Extraction.Text = ValeurColonne(Element.EntireRow, Colonne)
   '                 Set Colonne = GetColonne(LC_ORIGINE)
   '                 If Not Colonne Is Nothing Then Listing.Origine.Text = ValeurColonne(Element.EntireRow, Colonne)
   '                 If Not Listing.Extraction.Text = "" And Listing.Origine.Text = "" Then
   '                     Listing.Origine.Text = "{" & GetNomOnglet("DVSREPERECOMP", "ONGLETLISTEEQUIPEMENTS") & "}"
   '                     Listing.Origine.Locked = True
   '                 Else
    '                    Listing.Origine.Locked = False
   '                 End If
  '              Else
    '                Listing.Code.Visible = False
    '                Listing.Decoupage.Visible = False
    '                Listing.Equipement.Visible = False
    '                Listing.Extraction.Visible = False
    '                Listing.Origine.Visible = False
    '            End If
    '
    '            For Each i In Liste
    '                typelignedevis = DevisEnCours.CreationObjetLigne(i).TypeObjet
    '                If typelignedevis = "CHAPITRE" Or typelignedevis = "SOUSCHAPITRE" Then
    '                    Chaine = ValeurColonne(i.EntireRow, ColonneCode) & "-"
    '                ElseIf typelignedevis = "ENSGROUPE" Or _
    '                    typelignedevis = "ENSCOMPOSE" Or _
    '                    typelignedevis = "ARMOIRE" Then
    '                    Chaine = val(ValeurColonne(i.EntireRow, ColonneQuantite)) & " x "
    '                Else
    '                    Chaine = val(ValeurColonne(i.EntireRow, ColonneQuantite)) & " x "
    '                End If
    '                Chaine = Chaine & ValeurColonne(i.EntireRow, ColonneLibelle)  ' .Cells(1, 2).Value) '& Chr(13)
    '                If ValeurColonne(i.EntireRow, ColonneLibelle) <> "" Then
    '                    If val(ValeurColonne(i.EntireRow, ColonnePVU)) <> 0 Or typelignedevis = "CHAPITRE" Or typelignedevis = "SOUSCHAPITRE" Then
    '                        Set NodeEnCours = Listing.ListeLignes.Nodes.Add(, , "key:" & CStr(i.Row), Chaine)
    '                        Set NodeParent = Listing.ListeParents2.Nodes.Add(KeyNodeParent, tvwChild, "key:" & CStr(i.Row), Chaine)
    '                        Select Case typelignedevis
    '                            Case "CHAPITRE"
    '                                NodeParent.BackColor = RGB(254, 232, 230)
    '                                NodeParent.Bold = True
    '                            Case "SOUSCHAPITRE"
    '                                NodeParent.BackColor = RGB(255, 255, 213)
    '                            Case "ENSCOMPOSE"
    '                                NodeParent.BackColor = RGB(254, 232, 230)
    '                            Case "ENSGROUPE"
    '                                NodeParent.BackColor = RGB(221, 232, 251)
    '                        End Select
    '
    '                    Else
    '                        Set NodeEnCours = Listing.ListeLignes.Nodes.Add(, , "key:" & CStr(i.Row), ValeurColonne(i.EntireRow, ColonneLibelle))
    '                        NodeEnCours.Bold = True
    '                        Set NodeParent = Listing.ListeParents2.Nodes.Add(KeyNodeParent, tvwChild, "key:" & CStr(i.Row), Chaine) ' ValeurColonne(i.EntireRow, ColonneLibelle))
    '                    End If
    '                End If
    '            Next i
    '        Else
    '            Listing.Caption = ValeurColonne(Element.EntireRow, ColonneLibelle) 'CStr(Element.EntireRow.Cells(1, 2).Value)
'   '             If Not Element Is Nothing Then
'   '                 Chaine = CStr(Element.EntireRow.Cells(1, 2).Value) '& Chr(13)
'   '                 Listing.ListeElements.AddItem Chaine
'   '             End If
    '        End If
    '        If Listing.Visible = False Then
    '            Listing.Show 0
    '            If ListingTop <> 0 Then Listing.Top = ListingTop
    '            If ListingLeft <> 0 Then Listing.Left = ListingLeft
    '        End If
    '    End If
    '    AfficherDialogue = True
    'Else
    '    If Listing.Visible Then
    '        MemVisible = True
    '        ListingTop = Listing.Top
    '        ListingLeft = Listing.Left
    '        Listing.Hide
    '    End If
    'End If
End Function
Private Sub ConvertirVersPv()
Attribute ConvertirVersPv.VB_ProcData.VB_Invoke_Func = "b\n14"
    Dim Chapitres As Collection
    Dim SousChapitres As Collection
    Dim Elements As Collection
    Dim i As Range
    Dim j As Range
    Dim ws As Worksheet
    Dim ListeDesOnglets As New Collection
    Dim NouvelOngletPV As Boolean
    Dim ListeOngletSelect
    Dim ZoneSupRef As Range
    Dim Chaine As String
    Dim OngletOrigine As Worksheet
    Dim NouvelOnglet As Worksheet
    Dim OngletEnCours As Worksheet
    Dim ClasseurEnCours As Workbook
    Dim NouveauClasseur As Workbook
    Application.ScreenUpdating = False
    Application.DisplayAlerts = False
    For Each ws In ActiveWindow.SelectedSheets
        ws.Select
        If ValidationDVS Then
            ListeDesOnglets.Add ws
            If Not MacroSurPosteMaitre Then Exit For
        End If
    Next ws
On Error GoTo fin:
    If ListeDesOnglets.Count >= 1 Then
        If Not GetColonne(LC_EXTRACTION) Is Nothing Then If MacroSurPosteMaitre Then MiseAJourExtraction
        If MacroSurPosteMaitre Then MiseAJourBDL
        Application.StatusBar = "Creation bordereau prix de vente en cours..."
        If MsgBox("Voulez-vous convertir tout le devis en bordereau prix de vente ?", vbQuestion + vbYesNo) = vbYes Then
            BlocageModif = True
            BlocageListing = True
            ImpressionEnCours = True
            Application.Calculation = xlManual
            Application.Interactive = False
            Set ClasseurEnCours = ActiveWorkbook
            If DupliqueModele("BORDEREAU VIERGE.xls") Then
                Set NouvelOnglet = ActiveSheet
                Set NouveauClasseur = ActiveWorkbook
                For Each ws In ListeDesOnglets
                    If NouvelOngletPV Then
                        Call RajouteDepuisModele("BORDEREAU VIERGE.xls", NouveauClasseur)
                        Set NouvelOnglet = ActiveSheet
                    End If
                    ClasseurEnCours.Activate
                    ws.Select
                    Set ClasseurEnCours = ActiveWorkbook
                    Set OngletOrigine = ActiveSheet
                    OngletOrigine.Activate
                    OngletOrigine.Copy after:=ActiveSheet
                    Set OngletEnCours = ActiveSheet
                    OngletEnCours.Unprotect DevisPassWord
                    OngletEnCours.Cells(1, 1).Select
                    
                    Set ZoneSupRef = Range("A1", ColonnePVU.Cells(1, 1)).EntireColumn
                    ZoneSupRef.Select
                    ZoneSupRef.Copy
                    ZoneSupRef.PasteSpecial xlPasteValues
                    
                    'Suppression de toutes les formules dans la zone PR
                    'Transformation des ensembles en lignes
                    Set Chapitres = ListerElements(ActiveSheet.Cells(1, 1))
                    
                    'REPRENDRE CONVERSION EN PV PROBLEME DE FAMILLE
                    If Not Chapitres Is Nothing Then
                        For Each i In Chapitres
                            i.Select
                            Set SousChapitres = ListerElements(i)
                            If Not SousChapitres Is Nothing Then
                                For Each j In SousChapitres
                                    j.Select
                                    If Not DevisEnCours.ConvertirEnsembleVersElement(True) Then
                                        Set Elements = ListerElements(j)
                                        If Not Elements Is Nothing Then
                                            For Each k In Elements
                                                k.Select
                                                DevisEnCours.ConvertirEnsembleVersElement (True)
                                            Next k
                                        End If
                                    End If
                                Next j
                            End If
                        Next i
                    End If
                    OngletEnCours.Unprotect DevisPassWord
                    Application.Calculation = xlAutomatic
    '                Set ZoneSupRef = Range("A1", ColonnePVU.Cells(1, 1)).EntireColumn
    '                ZoneSupRef.Select
    '                ZoneSupRef.Copy
    '                ZoneSupRef.PasteSpecial xlPasteValues
                    ColonnePVU.Copy
                    ColonnePVU.PasteSpecial xlPasteValues
                    Range(ColonnesPR).Delete
                    If NomExiste("ZONEEXTRACTIONFAMILLES", ActiveSheet) Then
                        Range("ZONEEXTRACTIONFAMILLES").Delete
                    End If
                    Chaine = OngletEnCours.Name
                    If ColonnePVT2 Is Nothing Then
                        Range(ColonnePVT.Offset(0, 1), Selection.SpecialCells(xlCellTypeLastCell)).EntireColumn.Delete
                    Else
                        Range(ColonnePVT2.Offset(0, 1), Selection.SpecialCells(xlCellTypeLastCell)).EntireColumn.Delete
                    End If
                    ActiveSheet.Cells.Copy
                    NouvelOnglet.Activate
                    NouvelOnglet.Cells.PasteSpecial xlPasteAll
         '           For SautDePage = 1 To OngletEnCours.HPageBreaks.Count
         '               NouvelOnglet.HPageBreaks.Add OngletEnCours.HPageBreaks.Item(SautDePage).Location
         '           Next SautDePage
                    OngletEnCours.Delete
                    NouvelOnglet.Name = Chaine
        '//VERSION 1.2
                    If val(DevisVersion) >= 1.2 Then
                    Else
                        Columns("B:B").ColumnWidth = 57
                        Columns("C:C").ColumnWidth = 6.5
                        Columns("D:D").ColumnWidth = 12
                        Columns("E:E").ColumnWidth = 15
                    End If
        'END VERSION 1.2//
                    NouveauClasseur.Colors = ClasseurEnCours.Colors
                    ActiveWindow.DisplayZeros = False
                    OngletOrigine.Activate
                    OngletOrigine.Cells(1, 1).Select
                    If Not NouvelOngletPV And OngletExiste("Page de garde", ClasseurEnCours) Then
                        ClasseurEnCours.Sheets("Page de Garde").Copy Before:=NouveauClasseur.Sheets(1)
                    End If
                    NouvelOnglet.Protect
                    NouvelOnglet.Activate
                    NouvelOnglet.Cells(1, 1).Select
                    NouvelOngletPV = True
                Next ws
                ImpressionEnCours = False
                ActiveWindow.DisplayOutline = False
                BlocageModif = False
            Else
                MsgBox "Impossible d'imprimer le devis. Modele d'impression non présent.", vbCritical
            End If
        End If
    End If
fin:
On Error Resume Next
    Application.StatusBar = False
    BlocageListing = False
    BlocageModif = False
    ImpressionEnCours = False
    Application.Interactive = True
            Debug.Print Application.Interactive & Now()
End Sub
Private Function ListerElements(ByVal Origine As Object) As Collection
    Dim LaListeElements As New Collection
    Dim ListeFinale As New Collection
    Dim SousListe As New Collection
    If Not Origine Is Nothing Then Set LaListeElements = DevisEnCours.ListeDesElements(Origine)
    Set ListerElements = LaListeElements
'    Debug.Print origine.Value
'   If Not ListeElements Is Nothing Then
'        For Each i In ListeElements
'            ListeFinale.Add i
'            Set SousListe = ListerElements(i)
'            If Not souslisteisnothing Then
'                For Each j In SousListe
'                    ListeFinale.Add j
'                Next j
'            End If
'        Next i
'    End If
'    Set ListerElements = ListeFinale
End Function
Private Function ListeDesparents(Element As Range) As Collection
    Dim zone As Range
    Dim Liste As New Collection
    Dim lElement As Range
    Set lElement = Element
    Do Until lElement Is Nothing
        Set zone = DevisEnCours.TrouveParent(lElement)
        If Not zone Is Nothing Then
            Liste.Add zone
        End If
        Set lElement = zone
    Loop
    Set ListeDesparents = Liste
End Function
Private Sub AjouterMenu()
    Dim Menu As CommandBarControl
    Dim Libelle As CommandBarControl
    Dim BarreOutil As CommandBar
    Dim i As CommandBar
    Dim FichierIni As New clsFileIni
    FichierIni.SetFileName = RepertoireProgramme & "\GBDVS.ini"
    FichierIni.SetSection = "PARAMETRES"
    If Not MenuExiste Then
        Set BarreOutil = Application.CommandBars.Add(NomBarreOutil, , , True)
        BarreOutil.Protection = msoBarNoCustomize + msoBarNoChangeVisible
        Temp = FichierIni.ReadString("MenuRowIndex")
        If Temp <> "" Then
            BarreOutil.Position = FichierIni.ReadString("MenuPosition")
            BarreOutil.RowIndex = val(Temp)
            BarreOutil.Top = FichierIni.ReadString("MenuTop")
            BarreOutil.Left = FichierIni.ReadString("MenuLeft")
        Else
            For Each i In Application.CommandBars
                If i.Visible And i.RowIndex > 1 Then
                    BarreOutil.RowIndex = i.RowIndex + 1
                    Exit For
                End If
            Next i
        End If
        Set Menu = BarreOutil.Controls.Add(Type:=msoControlPopup, Temporary:=True)
        Menu.Caption = PersoBarreOutil
        Menu.BeginGroup = True
        Menu.Enabled = False
        Set Menu = BarreOutil.Controls.Add(Type:=msoControlPopup, Temporary:=True)
        Menu.Caption = "Edition"
        Menu.BeginGroup = True
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "Couper")
        Libelle.Caption = "Couper"
        Libelle.OnAction = "CouperZone"
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "Copier")
        Libelle.Caption = "Copier"
        Libelle.OnAction = "CopierZone"
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "Coller")
        Libelle.Caption = "Coller"
        Libelle.OnAction = "CollerZone"
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "ConvertirEvL")
        Libelle.Caption = "Convertir ensemble vers ligne"
        Libelle.OnAction = "ConvertirEnsembleVersElement"
        Libelle.BeginGroup = True
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "ConvertirLvE")
        Libelle.Caption = "Convertir ligne vers ensemble"
        Libelle.OnAction = "ConvertirElementVersEnsemble"
        
        If MacroSurPosteMaitre Then
            Set Libelle = Menu.Controls.Add(msoControlButton, 1, "RegrouperLibelles")
            Libelle.Caption = "Regrouper les libellés"
            Libelle.OnAction = "RegrouperLibelles"
            Libelle.BeginGroup = True
            Set Libelle = Menu.Controls.Add(msoControlButton, 1, "RegrouperLibellesListe")
            Libelle.Caption = "Création d'une liste des libellés"
            Libelle.OnAction = "RegrouperLibellesListe"
            Set Libelle = Menu.Controls.Add(msoControlButton, 1, "SupprimerTirets")
            Libelle.Caption = "Supprimer les tirets de début de ligne"
            Libelle.OnAction = "SupprimerTiret"
        End If
        
        Set Menu = BarreOutil.Controls.Add(Type:=msoControlPopup, Temporary:=True)
        Menu.Caption = "Insertion"
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "InsererChapitre")
        Libelle.Caption = "Insérer un chapitre"
        Libelle.OnAction = "InsererChapitre"
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "InsererSousChapitre")
        Libelle.Caption = "Insérer un sous-chapitre"
        Libelle.OnAction = "InsererSousChapitre"
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "InsererArmoire")
        Libelle.Caption = "Insérer une armoire"
        Libelle.OnAction = "InsererArmoire"
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "InsererLigne")
        Libelle.Caption = "Insérer une ligne"
        Libelle.OnAction = "InsererLigne"
'        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "InsererEtude")
'        Libelle.Caption = "F5 : Insérer ligne étude"
'        Libelle.OnAction = "InsererEtude"
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "InsererEnsGroupe")
        Libelle.Caption = "Insérer un article groupé"
        Libelle.OnAction = "InsererEnsGroupe"
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "InsererEnsCompose")
        Libelle.Caption = "Insérer un article composé"
        Libelle.OnAction = "InsererEnsCompose"
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "SupprimerZone")
        Libelle.BeginGroup = True
        Libelle.Caption = "Supprimer la sélection..."
        Libelle.OnAction = "SupprimerZone"
        
        If MacroSurPosteMaitre Then
            Set Libelle = Menu.Controls.Add(msoControlButton, 1, "RemonteLigne")
            Libelle.BeginGroup = True
            Libelle.Caption = "Remonte la sélection..."
            Libelle.OnAction = "RemonteLigne"
        End If
        
        Set Menu = BarreOutil.Controls.Add(Type:=msoControlPopup, Temporary:=True)
        Menu.Caption = "Outils"
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "ConvertirVersPv")
        Libelle.BeginGroup = True
        Libelle.Caption = "Création du bordereau en Pv..."
        Libelle.OnAction = "ConvertirVersPv"
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "MasquerLigne")
        Libelle.BeginGroup = True
        Libelle.Caption = "Masquer/Afficher le détail de la ligne"
        Libelle.OnAction = "MasquerLigne"
        Set Libelle = Menu.Controls.Add(msoControlButton, 1, "MasquerToutesZones")
        Libelle.Caption = "Masquer/Afficher tous les détails du devis"
        Libelle.OnAction = "MasquerToutesZones"
        
        'Set Libelle = Menu.Controls.Add(msoControlButton, 1, "ExtractFamilles")
        'Libelle.BeginGroup = True
        'Libelle.Caption = "Extraction des données par famille"
        'Libelle.OnAction = "ExtractFamilles"
        'If (val(DevisVersion) >= 2.4) Then Libelle.Enabled = True Else Libelle.Enabled = False
        
        If MacroSurPosteMaitre Then
            Set Libelle = Menu.Controls.Add(msoControlButton, 1, "AffichageDialogue")
            Libelle.Caption = "Afficher l'arborescence"
            Libelle.OnAction = "AffichageDialogue"
            Libelle.BeginGroup = True
        End If
        
        If MacroSurPosteMaitre Then
            Set Menu = BarreOutil.Controls.Add(Type:=msoControlPopup, Temporary:=True)
            Menu.Caption = "Données"
            Set Libelle = Menu.Controls.Add(msoControlButton, 1, "MiseAJourBD")
            Libelle.BeginGroup = True
            Libelle.Caption = "Mettre à jour les données du devis..."
            Libelle.OnAction = "MiseAJourBD"
            Set Libelle = Menu.Controls.Add(msoControlButton, 1, "MiseAJourMinutes")
            Libelle.Caption = "Mettre à jour les données venant des minutes..."
            Libelle.OnAction = "MiseAJourBDL"
            Set Libelle = Menu.Controls.Add(msoControlButton, 1, "MiseAJourExtraction")
            Libelle.BeginGroup = True
            Libelle.Caption = "Mettre à jour les données extraites..."
            Libelle.OnAction = "MiseAJourExtraction"
            Set Libelle = Menu.Controls.Add(msoControlButton, 1, "ValidationMaJ")
            Libelle.BeginGroup = True
            Libelle.Caption = "Supprimer les commentaires de mise à jour"
            Libelle.OnAction = "ValidationMiseAJour"
        End If
        If MacroSurPosteMaitre Then
            Set Menu = BarreOutil.Controls.Add(Type:=msoControlPopup, Temporary:=True)
            Menu.Caption = "Debug"
            Menu.BeginGroup = True
            Set Libelle = Menu.Controls.Add(msoControlButton, 1, "AfficheurNoms")
            Libelle.BeginGroup = True
            Libelle.Caption = "Gestionnaire des noms d'un classeur..."
            Libelle.OnAction = "AfficheurNoms"
            Set Libelle = Menu.Controls.Add(msoControlButton, 1, "VersionDevis")
            Libelle.Caption = "Version trame : " & ExtraitChaine(ActiveSheet.Names("XXX_DEVIS").RefersTo, "=", "")
            Libelle.Enabled = False
            If NomExiste("XXX_BDDEFAUT", ActiveSheet) Then
                Set Libelle = Menu.Controls.Add(msoControlButton, 1, "BD par défaut")
                Libelle.Caption = "BD par défaut : " & ExtraitChaine(ActiveSheet.Names("XXX_BDDEFAUT").RefersTo, "=", "")
                Libelle.Enabled = False
            End If
        End If
        BarreOutil.Visible = True
    End If
On Error Resume Next
    CommandBars("Worksheet Menu Bar").Controls("Edition").Controls("Couper").OnAction = "CopierValeur"
    CommandBars("Worksheet Menu Bar").Controls("Edition").Controls("Copier").OnAction = "CopierValeur"
    CommandBars("Worksheet Menu Bar").Controls("Edition").Controls("Coller").OnAction = "CollerValeur"
    CommandBars("Worksheet Menu Bar").Controls("Edition").Controls("Collage spécial...").Enabled = False
    CommandBars("Worksheet Menu Bar").Controls("Format").Controls("Mise en forme automatique...").Enabled = False
    CommandBars("Worksheet Menu Bar").Controls("Format").Controls("Mise en forme conditionnelle...").Enabled = False
    CommandBars("Worksheet Menu Bar").Controls("Fenêtre").Controls("Nouvelle fenêtre").Enabled = False
    CommandBars("Worksheet Menu Bar").Controls("Fenêtre").Controls("Supprimer le fractionnement").Enabled = False
    CommandBars("Worksheet Menu Bar").Controls("Fenêtre").Controls("Libérer les volets").Enabled = False
    CommandBars("Standard").Controls("Couper").Enabled = False
    CommandBars("Standard").Controls("Copier").Enabled = False
    CommandBars("Standard").Controls("Coller").Enabled = False
    CommandBars("Cell").Controls("Collage spécial...").Enabled = False
    CommandBars("Cell").Controls("Couper").OnAction = "CopierValeur"
    CommandBars("Cell").Controls("Copier").OnAction = "CopierValeur"
    CommandBars("Cell").Controls("Coller").OnAction = "CollerValeur"
    CommandBars("Row").Controls("Couper").Enabled = True
    CommandBars("Row").Controls("Couper").OnAction = "CouperZone"
    CommandBars("Row").Controls("Copier").OnAction = "CopierZone"
    CommandBars("Row").Controls("Coller").OnAction = "CollerZone"
    CommandBars("Row").Controls("Collage spécial...").Enabled = False
'    CommandBars("Row").Controls("Insertion").Enabled = True
'    CommandBars("Row").Controls("Insertion").OnAction = "InsererLigne"
'    CommandBars("Row").Controls("Supprimer...").Enabled = True
'    CommandBars("Row").Controls("Supprimer...").OnAction = "SupprimerZone"
'    CommandBars("Row").Controls("Supprimer").Enabled = True
'    CommandBars("Row").Controls("Supprimer").OnAction = "SupprimerZone"
    CommandBars("Row").Controls("Effacer le contenu").Enabled = False
'    CommandBars("Column").Controls("Couper").Enabled = False
'    CommandBars("Column").Controls("Copier").Enabled = False
'    CommandBars("Column").Controls("Coller").Enabled = False
'    CommandBars("Column").Controls("Collage spécial...").Enabled = False
End Sub
Private Sub SupprimerMenu()
On Error Resume Next
'    Application.CommandBars.ActiveMenuBar.Controls.Item("GFC-Construction").Delete
'    Application.CommandBars("GBDVS").Controls.Item("GFC-Construction").Delete
    Dim FichierIni As New clsFileIni
    FichierIni.SetFileName = RepertoireProgramme & "\GBDVS.ini"
    FichierIni.SetSection = "PARAMETRES"
    
    Call FichierIni.WriteString("MenuRowIndex", Application.CommandBars(NomBarreOutil).RowIndex)
    Call FichierIni.WriteString("MenuTop", Application.CommandBars(NomBarreOutil).Top)
    Call FichierIni.WriteString("MenuLeft", Application.CommandBars(NomBarreOutil).Left)
    Call FichierIni.WriteString("MenuPosition", Application.CommandBars(NomBarreOutil).Position)
    Application.CommandBars(NomBarreOutil).Delete
On Error Resume Next
    CommandBars("Worksheet Menu Bar").Controls("Edition").Controls("Couper").OnAction = ""
    CommandBars("Worksheet Menu Bar").Controls("Edition").Controls("Copier").OnAction = ""
    CommandBars("Worksheet Menu Bar").Controls("Edition").Controls("Coller").OnAction = ""
    CommandBars("Worksheet Menu Bar").Controls("Edition").Controls("Collage spécial...").Enabled = True
    CommandBars("Worksheet Menu Bar").Controls("Format").Controls("Mise en forme automatique...").Enabled = True
    CommandBars("Worksheet Menu Bar").Controls("Format").Controls("Mise en forme conditionnelle...").Enabled = True
    CommandBars("Worksheet Menu Bar").Controls("Fenêtre").Controls("Nouvelle Fenêtre").Enabled = True
    CommandBars("Worksheet Menu Bar").Controls("Fenêtre").Controls("Supprimer le fractionnement").Enabled = True
    CommandBars("Worksheet Menu Bar").Controls("Fenêtre").Controls("Libérer les volets").Enabled = True
    CommandBars("Worksheet Menu Bar").Controls("Fenêtre").Controls("Figer les volets").Enabled = True
    CommandBars("Standard").Controls("Couper").Enabled = True
    CommandBars("Standard").Controls("Copier").Enabled = True
    CommandBars("Standard").Controls("Coller").Enabled = True
    CommandBars("Cell").Controls("Collage spécial...").Enabled = True
    CommandBars("Cell").Controls("Couper").OnAction = ""
    CommandBars("Cell").Controls("Copier").OnAction = ""
    CommandBars("Cell").Controls("Coller").OnAction = ""
    CommandBars("Row").Controls("Couper").OnAction = ""
    CommandBars("Row").Controls("Copier").OnAction = ""
    CommandBars("Row").Controls("Coller").OnAction = ""
    CommandBars("Row").Controls("Collage spécial...").Enabled = True
'    CommandBars("Row").Controls("Insertion").OnAction = ""
'    CommandBars("Row").Controls("Insertion").Enabled = True
    CommandBars("Row").Controls("Supprimer...").OnAction = ""
    CommandBars("Row").Controls("Supprimer...").Enabled = True
    CommandBars("Row").Controls("Supprimer").OnAction = ""
    CommandBars("Row").Controls("Supprimer").Enabled = True
    CommandBars("Row").Controls("Supprimer Les lignes").OnAction = ""
    CommandBars("Row").Controls("Effacer le contenu").Enabled = True
'    CommandBars("Column").Controls("Couper").Enabled = True
'    CommandBars("Column").Controls("Copier").Enabled = True
'    CommandBars("Column").Controls("Coller").Enabled = True
'    CommandBars("Column").Controls("Collage spécial...").Enabled = True
    End Sub
Private Function MenuExiste() As Boolean
On Error GoTo fin
'    MenuExiste = Application.CommandBars.ActiveMenuBar.Controls.Item("GFC-Construction").Enabled
    MenuExiste = Application.CommandBars(NomBarreOutil).Enabled
    Exit Function
fin:
    Err.Clear
End Function
'Private Sub SaveParametre(NomCellule As String, Valeur As String)
'    If OngletExiste("Paramètres", Application.Workbooks(NomModuleMacro)) Then
'        If Not NomExiste(NomCellule, Application.Workbooks(NomModuleMacro).Worksheets("Paramètres")) Then
'            Call Application.Workbooks(NomModuleMacro).Worksheets("Paramètres").Names.Add(Name:=NomCellule, RefersTo:=Valeur, Visible:=False)
'        Else
'            Application.Workbooks(NomModuleMacro).Worksheets("Paramètres").Names(NomCellule).Value = Valeur
'        End If
'    End If
'End Sub
'Private Function LoadParametre(NomCellule As String) As String
'    If OngletExiste("Paramètres", Application.Workbooks(NomModuleMacro)) Then
'        If NomExiste(NomCellule, Application.Workbooks(NomModuleMacro).Worksheets("Paramètres")) Then
'            Temp = Application.Workbooks(NomModuleMacro).Worksheets("Paramètres").Names(NomCellule).Value
'            LoadParametre = Right(Temp, Len(Temp) - 1)
'        End If
'    End If
'End Function

Private Sub AfficheurNoms()
    AnalyseurDeNoms.Show 0
End Sub
Private Function DupliqueModele(NomModele As String) As Boolean
On Error GoTo fin:
    Dim NouveauClasseur As Workbook
    Set NouveauClasseur = Application.Workbooks.Open(CheminModeles & "\" & NomModele, , True)
    ActiveSheet.Copy
    NouveauClasseur.Close False
    DupliqueModele = True
fin:
End Function
Private Function RajouteDepuisModele(NomModele As String, ClasseurACompleter As Workbook) As Boolean
On Error GoTo fin:
    Dim NouveauClasseur As Workbook
    Set NouveauClasseur = Application.Workbooks.Open(CheminModeles & "\" & NomModele, , True)
    ActiveSheet.Copy after:=ClasseurACompleter.ActiveSheet
    NouveauClasseur.Close False
    RajouteDepuisModele = True
fin:
End Function

