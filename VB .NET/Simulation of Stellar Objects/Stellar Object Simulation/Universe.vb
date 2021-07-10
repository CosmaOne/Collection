﻿'MIT License

'Copyright (c) 2021 Kosmas Georgiadis

'Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

'The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

'THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


Public Class Universe

    Private universeGraphics As Graphics
    Private universePen As Pen
    Private bmpImage As Bitmap

    Private defaultFormWidth As Integer
    Private defaultFormHeight As Integer

    Private universeOffsetX As Double
    Private universeOffsetY As Double
    Private universeMatrix As Drawing2D.Matrix
    Private defaultUniverseMatrix As Drawing2D.Matrix
    Private resetOffset As Boolean

    Private universeWidth As Integer
    Private universeHeight As Integer

    Private visibleWidth As Integer
    Private visibleHeight As Integer

    Private planetList As New List(Of Planet)
    Private starList As New List(Of Star)

    Private canBounce As Boolean
    Private universeDragged As Boolean

    Private drawTrajectories As Boolean
    Private maxTrajPoints As Integer

    Private Const gravityConstant As Double = 6.674 * 10 ^ -11
    Private Const distanceMultiplier As Integer = 1000 'Multiply each pixel by this number.
    Private Const M As Double = 19890000000.0 ' = 1.989E+10 'This is my edited mass. The real Solar Mass = 1.989E+30

    Property OffsetX() As Double
        Get
            Return universeOffsetX
        End Get
        Set(ByVal Offset As Double)
            universeOffsetX = Offset
        End Set
    End Property
    Property OffsetY() As Double
        Get
            Return universeOffsetY
        End Get
        Set(ByVal Offset As Double)
            universeOffsetY = Offset
        End Set
    End Property

    Public ReadOnly Property getGraphics() As Graphics
        Get
            Return universeGraphics
        End Get
    End Property
    Public ReadOnly Property getPen() As Pen
        Get
            Return universePen
        End Get
    End Property
    Public ReadOnly Property getImage() As Bitmap
        Get
            Return bmpImage
        End Get
    End Property
    Public ReadOnly Property getWidth() As Integer
        Get
            Return universeWidth
        End Get
    End Property
    Public ReadOnly Property getHeight() As Integer
        Get
            Return universeHeight
        End Get
    End Property

    Public ReadOnly Property getResetOffsetFlag() As Boolean
        Get
            Return resetOffset
        End Get
    End Property

    Public ReadOnly Property Planets() As List(Of Planet)
        Get
            Return planetList
        End Get
    End Property
    Public ReadOnly Property Stars() As List(Of Star)
        Get
            Return starList
        End Get
    End Property
    Public ReadOnly Property Objects() As List(Of StellarObject)
        Get
            Dim objectList As New List(Of StellarObject)

            Try
                objectList.AddRange(starList)
                objectList.AddRange(planetList)
            Catch ex As Exception
                Console.WriteLine(ex.ToString)
            End Try
            Return objectList
        End Get
    End Property

    Public ReadOnly Property getGravityConstant() As Double
        Get
            Return gravityConstant
        End Get
    End Property
    Public ReadOnly Property getDistanceMultiplier() As Integer
        Get
            Return distanceMultiplier
        End Get
    End Property

    Public ReadOnly Property drawTraj() As Boolean
        Get
            Return drawTrajectories
        End Get
    End Property
    Public ReadOnly Property getMaxTrajPoints() As Integer
        Get
            Return maxTrajPoints
        End Get
    End Property

    ' Allow friend access to the empty constructor.
    Friend Sub New()
    End Sub

    Friend Sub Init(ByVal bGraphics As Graphics, ByVal bPen As Pen, ByVal uWidth As Integer, ByVal uHeight As Integer,
                    ByVal fWidth As Integer, ByVal fHeight As Integer, ByVal offset As PointF,
                    ByVal drawTraj As Boolean, ByVal _maxTrajPoints As Integer, ByVal bounce As Boolean)
        universePen = bPen
        universeOffsetX = offset.X
        universeOffsetY = offset.Y

        universeWidth = uWidth
        universeHeight = uHeight

        'Set current default form values.
        SetDefaultFormWidth(fWidth)
        SetDefaultFormHeight(fHeight)

        bmpImage = New Bitmap(universeWidth, universeHeight) 'Create image. Take in account the DPI factor too.

        universeGraphics = Graphics.FromImage(bmpImage)
        universeGraphics.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
        universeGraphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        'Save transformation so we don't run into access violations.
        universeMatrix = universeGraphics.Transform.Clone()
        universeMatrix.Invert()

        Dim visibleBounds As RectangleF = bmpImage.GetBounds(universeGraphics.PageUnit)
        visibleWidth = visibleBounds.Width
        visibleHeight = visibleBounds.Height

        universeGraphics.Clip = New Region(New RectangleF(visibleBounds.Left + universeOffsetX, visibleBounds.Top + universeOffsetY,
                                                          visibleWidth, visibleHeight))

        drawTrajectories = drawTraj 'Draw trajectories?
        maxTrajPoints = _maxTrajPoints 'Max number of points used for each trajectory.
        universeDragged = False 'Flag to check if the universe is being dragged.

    End Sub
    Friend Sub Live(ByRef starArrayUse As Boolean, ByRef planetArrayUse As Boolean,
                    ByRef paintingStars As Boolean, ByRef paintingPlanets As Boolean)

        applyAcceleration(starArrayUse, planetArrayUse, paintingStars, paintingPlanets)

    End Sub

    Friend Sub AddPlanet(ByVal newplanet As Planet)
        planetList.Add(newplanet)
    End Sub
    Friend Sub AddStar(ByVal newstar As Star)
        starList.Add(newstar)
    End Sub

    'Friend Sub SetRightBottomBoundary(ByVal point As PointF)
    '    right_bottom_boundary(0) = point
    'End Sub
    'Friend Sub SetLeftTopBoundary(ByVal point As PointF)
    '    left_top_boundary(0) = point
    'End Sub
    Friend Sub SetDefaultFormWidth(ByVal width As Integer)
        defaultFormWidth = width
    End Sub
    Friend Sub SetDefaultFormHeight(ByVal height As Integer)
        defaultFormHeight = height
    End Sub

    Friend Sub SetBounceStatus(ByVal status As Boolean)
        canBounce = status
    End Sub
    Friend Sub SetDragStatus(ByVal status As Boolean)
        universeDragged = status
    End Sub
    Friend Sub SetTrajStatus(ByVal status As Boolean)
        drawTrajectories = status
    End Sub
    Friend Sub SetTrajMaxPoints(ByVal value As Integer)
        maxTrajPoints = value
    End Sub

    Friend Sub applyAcceleration(ByRef starArrayUse As Boolean, ByRef planetArrayUse As Boolean,
                            ByRef paintingStars As Boolean, ByRef paintingPlanets As Boolean)

        While paintingStars Or starArrayUse Or planetArrayUse Or paintingPlanets
        End While

        'If the offset is changed due to zooming, set the new offset for all stellar objects and check if they are still visible.

        If resetOffset Then

            starArrayUse = True
            planetArrayUse = True

            ResetAllOffsets(starArrayUse, planetArrayUse, paintingStars, paintingPlanets) 'Start resetting.
            resetOffset = False 'Clear global flag.

            starArrayUse = False
            planetArrayUse = False

        End If

        'First, calculate acceleration from all stars, except the merged ones.
        starArrayUse = True
        planetArrayUse = True

        For Each star In starList.FindAll(Function(s) s.IsMerged = False)

            If star.IsMerged Then Continue For 'Check if a star merged while looping.
            star.applyAcceleration(Objects(), gravityConstant)

        Next

        'Now move them.
        For Each star In starList.FindAll(Function(s) s.IsMerged = False)

            Dim newpos As New PointF(star.VelX, star.VelY) 'Set new center of mass.
            star.Move(0, "", star.CenterOfMass.X + newpos.X, star.CenterOfMass.Y + newpos.Y) 'Move star.

            If canBounce And ((universeDragged And star.isPartialVisible) Or Not star.IsOutOfBounds) Then
                star.CheckForBounce()
            End If

        Next

        starArrayUse = False
        planetArrayUse = False

        While paintingPlanets Or planetArrayUse
        End While

        'Do the same for the planets.
        planetArrayUse = True

        Try
            For Each planet In planetList

                If planet.IsMerged = False Then Continue For  'Check if a planet merged while looping.
                planet.applyAcceleration(starList, gravityConstant)

            Next

            'Now move them.
            For Each planet In planetList

                If planet.IsMerged = False Then

                    Dim newpos As New PointF(planet.VelX, planet.VelY)
                    planet.Move(0, "", planet.CenterOfMass.X + newpos.X - planet.Size / 2, planet.CenterOfMass.Y + newpos.Y - planet.Size / 2) 'Move planet.

                    If canBounce And ((universeDragged And planet.isPartialVisible) Or Not planet.IsOutOfBounds) Then
                        planet.CheckForBounce()
                    End If

                End If

            Next
        Catch ex As Exception
            Console.Write(ex.ToString)
        End Try

        planetArrayUse = False

    End Sub

    Friend Sub ResizeUniverse(ByVal clipOffset As PointF, ByVal universeSize As PointF, ByVal matrix As Drawing2D.Matrix)

        'Update local values.
        universeMatrix = matrix
        universeOffsetX = clipOffset.X
        universeOffsetY = clipOffset.Y
        universeWidth = universeSize.X
        universeHeight = universeSize.Y

        resetOffset = True

    End Sub
    Friend Sub ResetAllOffsets(ByRef starArrayUse As Boolean, ByRef planetArrayUse As Boolean,
                            ByRef paintingStars As Boolean, ByRef paintingPlanets As Boolean)

        'universeMatrix.Invert() 'This happens for a new universe matrix, every time we zoom/move the universe. In other words, the same matrix won't be inverted twice or more.

        For Each star In starList

            If star.IsMerged Then
                Continue For
            End If

            'Set new offset.
            star.UniverseOffsetX = universeOffsetX
            star.UniverseOffsetY = universeOffsetY

            'Check for bounce only on zoom.
            'This will prevent the bounce effect even if it's halfway through the wall.
            If canBounce And Not universeDragged Then

                If Not star.isFullyVisible Then
                    star.IsOutOfBounds = True
                Else
                    star.IsOutOfBounds = False 'Reset if visible again.
                End If

            ElseIf Not canBounce Then 'For tunneling, we don't do it, if it's more than half out of bounds.

                If Not star.isPartialVisible Then
                    star.IsOutOfBounds = True
                Else
                    star.IsOutOfBounds = False 'Reset if visible again.
                End If

            End If

            'Update universe matrix for the object.
            star.UniverseMatrix = universeMatrix.Clone

            'Transform original point.
            Dim points() As PointF = {star.OriginPoint}
            Dim inverseMatrix = universeMatrix.Clone
            inverseMatrix.Invert()
            inverseMatrix.TransformPoints(points)

        Next

        For Each planet In planetList
            If planet.IsMerged Then
                Continue For
            End If

            'Set new offset.
            planet.UniverseOffsetX = universeOffsetX
            planet.UniverseOffsetY = universeOffsetY

            'Check for bounce only on zoom.
            'This will prevent the bounce effect even if it's halfway through the wall.
            If canBounce And Not universeDragged Then

                If Not planet.isFullyVisible Then
                    planet.IsOutOfBounds = True
                Else
                    planet.IsOutOfBounds = False 'Reset if visible again.
                End If

            ElseIf Not canBounce Then 'For tunneling, we don't do it, if it's more than half out of bounds.

                If Not planet.isPartialVisible Then
                    planet.IsOutOfBounds = True
                Else
                    planet.IsOutOfBounds = False 'Reset if visible again.
                End If

            End If

            'Update universe matrix for the object.
            planet.UniverseMatrix = universeMatrix.Clone

            'Transform original point.
            Dim points() As PointF = {planet.OriginPoint}
            Dim inverseMatrix = universeMatrix.Clone
            inverseMatrix.TransformPoints(points)

            If drawTrajectories Then

                Dim tPoints = planet.TrajectoryPoints.ToArray
                inverseMatrix.TransformPoints(tPoints)
                planet.TrajectoryPoints = tPoints.ToList

            End If
        Next

    End Sub

End Class

