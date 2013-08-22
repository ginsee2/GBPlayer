Public Class WaveFormTimeLine
    Public Property DureeTotale As Double
    Public Property PositionPlay As Double
    ' Public Property FileName As String
    ' Private LeftPath As Path = New Path
    Private DataPath As Path = New Path
    Private LinePositionRec As Line = New Line
    Private LinePositionPlay As Line = New Line
    Private LinePositionPlay2 As Line = New Line
    Private RectangleSelection As Rectangle = New Rectangle
    Private Sub WaveFormTimeLine_Initialized(sender As Object, e As EventArgs) Handles Me.Initialized
        waveformCanvas.Height = Me.Height
        waveformCanvas.Width = Me.Width
        waveformCanvas.Children.Add(LinePositionRec)
        waveformCanvas.Children.Add(LinePositionPlay)
        waveformCanvas.Children.Add(LinePositionPlay2)
        waveformCanvas.Children.Add(RectangleSelection)
        waveformCanvas.Children.Add(DataPath)
        LinePositionRec.Stroke = New SolidColorBrush(System.Windows.Media.Colors.OrangeRed)
        LinePositionPlay.Stroke = New SolidColorBrush(System.Windows.Media.Colors.Blue)
        LinePositionPlay2.Stroke = New SolidColorBrush(System.Windows.Media.Colors.Olive)
        RectangleSelection.Fill = New SolidColorBrush(System.Windows.Media.Colors.BlueViolet)
        RectangleSelection.Opacity = 0.8
    End Sub

    'Dim DemandeMaJ As Long
    'Private Delegate Sub SelectionChangedDelegate(ByVal NomFichier As String, ByVal NumMiseAJour As Long)
    'Private Delegate Sub InitAffichageDelegate(ByVal NomFichier As String, ByVal Longueur As Long, ByVal NumMiseAJour As Long)
    'Private Delegate Sub MiseAJourAffichageDelegate(ByVal NomFichier As String, ByVal Position As Double, ByVal Infos() As Int16, ByVal NumMiseAJour As Long)
    'Public Sub update()
    'DemandeMaJ += 1
    'Dim ReadTag As New SelectionChangedDelegate(AddressOf ListeFichiersMp3SelectionChanged)
    'ReadTag.BeginInvoke(FileName, DemandeMaJ, Nothing, Nothing)
    'End Sub
    'Private Sub ListeFichiersMp3SelectionChanged(ByVal NomFichier As String, ByVal NumMiseAJour As Long)
    ' Dim Infos() As Int16
    ' Dim File As BassStream = New BassStream(1, NomFichier, Enum_Bass_StreamCreate.BASS_STREAM_DECODE)
    ' If File IsNot Nothing Then
    ' If File.ChannelDuration > 0 Then
    ' If NumMiseAJour = DemandeMaJ Then _
    ' Me.Dispatcher.BeginInvoke(New InitAffichageDelegate(AddressOf InitAffichage),
    '                             System.Windows.Threading.DispatcherPriority.Input, {NomFichier, File.ChannelLenght, NumMiseAJour})
    ' Do
    ' Infos = File.ReadDataInt(100000)
    ' If Infos IsNot Nothing Then _
    ' Me.Dispatcher.BeginInvoke(New MiseAJourAffichageDelegate(AddressOf ListeFichiersMiseAJourAffichage),
    '                            System.Windows.Threading.DispatcherPriority.Input, {NomFichier, File.ChannelPositionInSecond, Infos, NumMiseAJour})
    ' Loop Until Infos Is Nothing
    ' End If
    ' File.Close()
    ' End If
    'End Sub
    Const minValue = -(2 ^ 16) / 2
    Const maxValue = (2 ^ 16) / 2
    Const dbScale = (maxValue - minValue)
    Dim xLocation As Double = 0.0
    '    Dim rightGeometry As PathGeometry = New PathGeometry()
    ' Dim rightPathFigure As PathFigure = New PathFigure()
    ' Private Sub InitAffichage(ByVal NomFichier As String, ByVal Longueur As Long, ByVal NumMiseAJour As Long)
    '     If NumMiseAJour = DemandeMaJ Then
    '         LibelleContenu.Content = FileName
    '         LeftPath.Data = Nothing
    '         RightPath.Data = Nothing
    '         rightWaveformPolyLine = New PolyLineSegment()
    '         xLocation = 0
    '         FileLenght = Longueur
    '     End If
    ' End Sub
    'Private DataPathGeometry As PathGeometry
    'Private DataPathFigure As PathFigure
    Private DataWaveformPolyLine As PolyLineSegment
    Private CollectionPolyLine As List(Of PolyLineSegment)
    Public Property SegmentsTotal As Integer
    Private SegmentEnCours As Integer
    Public Property SegmentAffiche As Integer
    Dim SegmentPlay As Integer
    Private DureeIndex As Double
    Private Regroupement As Double
    Private PointThickness As Double
    Private TimeOfPixel As Double
    Public Property DureeSegment As Double = 20
    Public Sub InitAffichage()
        Dim FileLenght As Double = DureeSegment * (44100 * 2)
        Dim pointCount As Double = CInt(FileLenght / 2)
        PointThickness = waveformCanvas.RenderSize.Width / pointCount
        TimeOfPixel = (waveformCanvas.RenderSize.Width) / DureeSegment
        Dim Diviseur As Integer = 100 / (1 / PointThickness)
        If Diviseur = 0 Then Diviseur = 4
        Regroupement = Int((1 / PointThickness) / Diviseur)
        If Regroupement > 0 Then
            PointThickness = PointThickness * Regroupement
        End If
        ' DureeSegment = waveformCanvas.RenderSize.Width / PointThickness / Regroupement * 8
        'Regroupement = Int(Regroupement)
        DataPath.Data = Nothing
        CollectionPolyLine = New List(Of PolyLineSegment)
        DataWaveformPolyLine = New PolyLineSegment()
        CollectionPolyLine.Add(DataWaveformPolyLine)
        xLocation = 0
        SegmentEnCours = 0
        SegmentsTotal = 0
        SegmentPlay = 0
        PositionPlay = 0
        DureeIndex = 0
        DureeTotale = 0
    End Sub
    Public Sub AddData(ByVal Position As Double, ByVal Infos() As Int16, Optional MonoSegment As Boolean = False)
        If (Position <> 0) And (Infos.Length > 0) Then
            If Infos IsNot Nothing Then
                Dim Compteur As Integer = 0
                Dim ValeurMax As Single = minValue
                Dim ValeurMin As Single = maxValue
                Dim waveformSideHeight As Double = waveformCanvas.RenderSize.Height / 2.0
                Dim centerHeight As Double = waveformSideHeight
                Dim rightRenderHeight As Double = 0
                DataWaveformPolyLine.Points.Add(New Point(xLocation, centerHeight))
                Dim TempsAjoute As Integer = 0
                For i = 0 To Infos.Length - 2 Step 2
                    ValeurMax = Math.Max(ValeurMax, Infos(i))
                    ValeurMin = Math.Min(ValeurMin, Infos(i))
                    Dim FinIndex As Boolean = False
                    TempsAjoute += 2
                    DureeIndex += 2
                    If DureeIndex = (44100 * 2 * DureeSegment) Then
                        DureeIndex = 0
                        FinIndex = True
                    Else
                    End If

                    If (Compteur = Regroupement) Or (i = Infos.Length - 2) Or ((FinIndex) And (Not MonoSegment)) Then
                        If Math.Abs(ValeurMin) > Math.Abs(ValeurMax) Then ValeurMax = ValeurMin
                        If Regroupement > 0 Then
                            xLocation += PointThickness * (Compteur / Regroupement)
                        Else
                            xLocation += PointThickness
                        End If
                        rightRenderHeight = ((ValeurMax / dbScale) * waveformSideHeight * 1.3)
                        DataWaveformPolyLine.Points.Add(New Point(xLocation, centerHeight + rightRenderHeight))
                        DataWaveformPolyLine.Points.Add(New Point(xLocation, centerHeight - rightRenderHeight))
                        If (MonoSegment And (xLocation > Me.ActualWidth)) Then Exit For
                        If (FinIndex) And (Not MonoSegment) Then
                            Debug.Print(Position.ToString)
                            xLocation = 0
                            SegmentEnCours += 1
                            SegmentsTotal += 1
                            DataWaveformPolyLine = New PolyLineSegment()
                            CollectionPolyLine.Add(DataWaveformPolyLine)
                        End If
                        ValeurMax = minValue
                        ValeurMin = maxValue
                        Compteur = 0
                    Else
                        Compteur += 1
                    End If
                Next
                DureeTotale += Infos.Length / (44100 * 2)
                AffichageSegment(SegmentEnCours)
                LinePositionRec.X1 = (DureeTotale - SegmentEnCours * DureeSegment) * TimeOfPixel '(DureeIndex / (44100 * 2)) * TimeOfPixel '  ' 
                LinePositionRec.X2 = LinePositionRec.X1
                LinePositionRec.Y1 = 0
                LinePositionRec.Y2 = Height
            End If
        End If
    End Sub
    Public Sub AffichageSegment(NumSegment As Integer)
        If (DureeTotale > 0) And (NumSegment >= 0) And (NumSegment <= SegmentsTotal) Then
            Dim DataPathGeometry = New PathGeometry()
            Dim DataPathFigure = New PathFigure()
            DataPathFigure.Segments.Add(CollectionPolyLine.Item(NumSegment))
            DataPathGeometry.Figures.Add(DataPathFigure)
            DataPath.Data = DataPathGeometry
            DataPath.Stroke = New SolidColorBrush(Colors.White)
            SegmentAffiche = NumSegment
            If NumSegment <> SegmentsTotal Then
                LinePositionRec.Visibility = Windows.Visibility.Hidden
            Else
                LinePositionRec.Visibility = Windows.Visibility.Visible
            End If
            If (NumSegment <> SegmentPlay) Or (PositionPlay = 0) Then
                LinePositionPlay.Visibility = Windows.Visibility.Hidden
                LinePositionPlay2.Visibility = Windows.Visibility.Hidden
                RectangleSelection.Visibility = Windows.Visibility.Hidden
            Else
                LinePositionPlay.Visibility = Windows.Visibility.Visible
                LinePositionPlay2.Visibility = Windows.Visibility.Visible
                RectangleSelection.Visibility = Windows.Visibility.Visible
            End If
        End If
    End Sub
    Public Function AffichagePlay(Position As Double) As Boolean
        If DureeTotale > 0 Then
            PositionPlay = Position / 44100 / 4
            If PositionPlay > DureeTotale Then Return False
            SegmentPlay = Int(PositionPlay / DureeSegment)
            AffichageSegment(SegmentPlay)
            LinePositionPlay.X1 = (PositionPlay - SegmentPlay * DureeSegment) * TimeOfPixel '(DureeIndex / (44100 * 2)) * TimeOfPixel '  ' 
            LinePositionPlay.X2 = LinePositionPlay.X1
            LinePositionPlay.Y1 = 0
            LinePositionPlay.Y2 = Height
            Return True
        End If
        Return False
    End Function
    Dim EditionPositionEnCours As Boolean
    Dim DessinPositionEnCours As Boolean
    Dim PointDepart As Point
    Dim PointArrivee As Point
    Private LinePositionSouris As Line = New Line
    Private Sub EditeurTAG_PreviewMouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles waveformCanvas.PreviewMouseLeftButtonDown
        ' If EditionPositionEnCours Then
        PointDepart = e.GetPosition(waveformCanvas)
        waveformCanvas.CaptureMouse()
        DessinPositionEnCours = True
        ' End If
    End Sub
    Private Sub WaveFormTimeLine_MouseMove(sender As Object, e As MouseEventArgs) Handles waveformCanvas.MouseMove
        If (DessinPositionEnCours) Then
            PointArrivee.X = e.GetPosition(waveformCanvas).X
            PointArrivee.Y = e.GetPosition(waveformCanvas).Y
            If PointArrivee.X > PointDepart.X Then
                LinePositionPlay.Visibility = Windows.Visibility.Visible
                LinePositionPlay.X1 = PointDepart.X
                LinePositionPlay.X2 = LinePositionPlay.X1
                LinePositionPlay.Y1 = 0
                LinePositionPlay.Y2 = Height
                LinePositionPlay2.Visibility = Windows.Visibility.Visible
                LinePositionPlay2.X1 = PointArrivee.X
                LinePositionPlay2.X2 = LinePositionPlay2.X1
                LinePositionPlay2.Y1 = 0
                LinePositionPlay2.Y2 = Height
            Else
                LinePositionPlay.Visibility = Windows.Visibility.Visible
                LinePositionPlay.X1 = PointArrivee.X
                LinePositionPlay.X2 = LinePositionPlay.X1
                LinePositionPlay.Y1 = 0
                LinePositionPlay.Y2 = Height
                LinePositionPlay2.Visibility = Windows.Visibility.Visible
                LinePositionPlay2.X1 = PointDepart.X
                LinePositionPlay2.X2 = LinePositionPlay2.X1
                LinePositionPlay2.Y1 = 0
                LinePositionPlay2.Y2 = Height
            End If
            RectangleSelection.Visibility = Windows.Visibility.Visible
            RectangleSelection.Height = Height
            RectangleSelection.Width = LinePositionPlay2.X1 - LinePositionPlay.X1
            Canvas.SetTop(RectangleSelection, 0)
            Canvas.SetLeft(RectangleSelection, LinePositionPlay.X1)
            PositionPlay = SegmentAffiche * DureeSegment + LinePositionPlay.X1 / TimeOfPixel
            SegmentPlay = Int(PositionPlay / DureeSegment)
        End If

    End Sub
    Private Sub EditeurTAG_PreviewMouseLeftButtonUp(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonUp
        If DessinPositionEnCours Then
            If waveformCanvas.IsMouseCaptured Then
                waveformCanvas.ReleaseMouseCapture()
            End If
            DessinPositionEnCours = False
        End If
    End Sub

End Class
