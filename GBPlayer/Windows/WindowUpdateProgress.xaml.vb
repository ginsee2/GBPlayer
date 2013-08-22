'***********************************************************************************************
'---------------------------------TYPES PUBLIC DE LA CLASSE------------------------------------
'***********************************************************************************************
Public Class WindowUpdateProgress
    Private Fenetre As MainWindow
    Private FenetreInfo As WindowUpdateProgress
    Private Property TopFenetreParente As Integer
    Private Property LeftFenetreParente As Integer
    Public Property FenetreAffichee As Boolean
    Private Compteur As Integer
    Private Max As Integer
    Private ListeProcess As Dictionary(Of Long, Integer) = New Dictionary(Of Long, Integer)
    Private ListeCompteurs As Dictionary(Of Long, Integer) = New Dictionary(Of Long, Integer)
    Private NumProcess As Long
    Public Sub New()
        ' Cet appel est requis par le concepteur.
        InitializeComponent()
        ' Ajoutez une initialisation quelconque après l'appel InitializeComponent().
    End Sub

    Public Sub New(ByVal DisplayWindows As MainWindow)
        InitializeComponent()
        Fenetre = DisplayWindows
        Owner = Fenetre
        If Fenetre Is Nothing Then
            TopFenetreParente = 0
            LeftFenetreParente = 0
        Else
            TopFenetreParente = Fenetre.PointToScreen(Fenetre.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Fenetre)).Y
            LeftFenetreParente = Fenetre.PointToScreen(Fenetre.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Fenetre)).X
        End If
    End Sub
    Public Function AddNewProcess(ProcessNumberToAdd As Integer, Optional UpdateProcess As Long = 0) As Long
        If ListeProcess.ContainsKey(UpdateProcess) Then
            ListeProcess.Item(NumProcess) += ProcessNumberToAdd
            Max += ProcessNumberToAdd
            NbrTotal.Text = Max ' CInt(NbrTotal.Text) + ProcessNumberToAdd '  maxi.ToString ' (compteur + _ListeTacheAExecuter.Count - 1).ToString
            Return UpdateProcess
        Else
            NumProcess += 1
            ListeProcess.Add(NumProcess, ProcessNumberToAdd)
            ListeCompteurs.Add(NumProcess, 0)
            Max += ProcessNumberToAdd
            NbrTotal.Text = Max ' CInt(NbrTotal.Text) + ProcessNumberToAdd '  maxi.ToString ' (compteur + _ListeTacheAExecuter.Count - 1).ToString
            Return NumProcess
        End If
    End Function
    Public Sub UpdateWindows(ByVal NomFichier As String, NumProcess As Long, Optional ByVal OtherMessage As Boolean = False, Optional Avancement As Integer = 0)
        If ListeProcess.ContainsKey(NumProcess) Then
            If Not OtherMessage Then Compteur += 1
            If Not FenetreAffichee Then
                WindowStartupLocation = WindowStartupLocation.Manual
                TopFenetreParente = Fenetre.PointToScreen(Fenetre.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Fenetre)).Y
                LeftFenetreParente = Fenetre.PointToScreen(Fenetre.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Fenetre)).X
                Top = TopFenetreParente
                Left = LeftFenetreParente
                Height = Fenetre.BordureAffichageTemp.ActualHeight - 4
                Width = Fenetre.BordureAffichageTemp.ActualWidth - 5 '- Fenetre.PointToScreen(Fenetre.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Fenetre)).Y
                Me.Show()
                FenetreAffichee = True
            End If
            ' If ((Not OtherMessage) And (Compteur >= Max)) Or (OtherMessage And (Max = 0) And (NomFichier = "")) Then
            ' Me.Visibility = Windows.Visibility.Collapsed
            ' FenetreAffichee = False
            ' Max = 0
            ' Compteur = 0
            ' Exit Sub
            'End If
            If Not OtherMessage Then ListeCompteurs.Item(NumProcess) += 1
            NumEnCours.Text = Compteur
            NbrTotal.Text = Max ' maxi.ToString ' (compteur + _ListeTacheAExecuter.Count - 1).ToString
            ProgressionTraitement.Maximum = CInt(NbrTotal.Text.ToString)
            ProgressionTraitement.Value = Compteur
            NomFichierEnCoursTraitement.Text = NomFichier
            If Avancement > 0 And Avancement < 100 Then
                AvancementProcess.Visibility = Windows.Visibility.Visible
                AvancementProcess.Maximum = 100
                AvancementProcess.Value = Avancement
            Else
                AvancementProcess.Visibility = Windows.Visibility.Hidden
            End If
        End If
    End Sub
    Public Sub StopProcess(NumProcess As Long)
        If ListeProcess.ContainsKey(NumProcess) Then
            If ListeCompteurs.Item(NumProcess) = ListeProcess.Item(NumProcess) Then
                Compteur -= ListeCompteurs.Item(NumProcess)
                Max -= ListeProcess.Item(NumProcess)
                NbrTotal.Text = Max ' CInt(NbrTotal.Text) + ProcessNumberToAdd '  maxi.ToString ' (compteur + _ListeTacheAExecuter.Count - 1).ToString
                ListeProcess.Remove(NumProcess)
                ListeCompteurs.Remove(NumProcess)
                If Max = 0 Then
                    Me.Visibility = Windows.Visibility.Collapsed
                    FenetreAffichee = False
                    Max = 0
                    Compteur = 0
                    Exit Sub
                End If
            End If
        End If
    End Sub

End Class
