Namespace AutoWikiBrowser.Plugins.SDKSoftware.Kingbotk
    ''' <summary>
    ''' An object representing an article which may or may not contain the targetted template
    ''' </summary>
    Friend NotInheritable Class Article
        ' Properties:
        Private mArticleText As String, mFullArticleTitle As String
        Private mArticleTitle As String, mNamespace As Namespaces, mHavePlacedATemplateAtTop As Boolean
        Private mEditSummary As String = PluginManager.conWikiPluginBrackets, mMajor As Boolean

        ' Plugin-state:
        Private mSkipResults As SkipResults = SkipResults.NotSet
        Private mProcessIt As Boolean ' gets set by ArticleHasAMajorChange/ArticleHasAMinorChange

        ' New:
        Friend Sub New(ByVal ArticleText As String, ByVal vFullArticleTitle As String, _
        ByVal vNamespace As Namespaces)
            mArticleText = ArticleText
            mFullArticleTitle = vFullArticleTitle
            mNamespace = vNamespace
            'mFullArticleTitle = GetArticleName(mNamespace, mArticleTitle)
        End Sub

        ' Public properties:
        Public Property AlteredArticleText() As String
            Get
                Return mArticleText
            End Get
            Set(ByVal value As String)
                mArticleText = value
            End Set
        End Property
        Public ReadOnly Property FullArticleTitle() As String
            Get
                Return mFullArticleTitle
            End Get
        End Property
        Public ReadOnly Property [Namespace]() As Namespaces
            Get
                Return mNamespace
            End Get
        End Property
        Public Property EditSummary() As String
            Get
                Return mEditSummary
            End Get
            Set(ByVal value As String)
                mEditSummary = value
            End Set
        End Property
        Public Sub ArticleHasAMinorChange()
            mProcessIt = True
        End Sub
        Public Sub ArticleHasAMajorChange()
            mProcessIt = True
            mMajor = True
        End Sub
        Public ReadOnly Property ProcessIt() As Boolean
            Get
                Return mProcessIt
            End Get
        End Property
        'Public ReadOnly Property Major() As Boolean
        '    Get
        '        Return mMajor
        '    End Get
        'End Property
        Public Sub HavePlacedATemplateAtTop()
            mHavePlacedATemplateAtTop = True
        End Sub

        ' For calling by plugin:
        Friend Sub PluginCheckTemplateCall(ByVal TemplateCall As String, ByVal PluginName As String)
            If Not TemplateCall = "" Then ' we have "template:"
                mProcessIt = True
                EditSummary += "Remove ""template:"", "
                PluginSettingsControl.MyTrace.WriteArticleActionLine("Remove ""template:"" call", PluginName)
            End If
        End Sub
        Friend Sub PluginIHaveFinished(ByVal Result As SkipResults, ByVal PluginName As String)
            Select Case Result
                Case SkipResults.SkipBadTag
                    mSkipResults = SkipResults.SkipBadTag
                    PluginSettingsControl.MyTrace.SkippedArticleBadTag(PluginName, mFullArticleTitle, mNamespace)
                Case SkipResults.SkipMiscellaneous
                    If mSkipResults = SkipResults.NotSet Then mSkipResults = SkipResults.SkipMiscellaneous
                    PluginSettingsControl.MyTrace.SkippedArticle(PluginName, "")
                Case SkipResults.SkipNoChange
                    PluginSettingsControl.MyTrace.SkippedArticle(PluginName, "No change")
                    mSkipResults = SkipResults.SkipNoChange
            End Select
        End Sub

        ' For calling by manager:
        Friend ReadOnly Property PluginManagerGetSkipResults() As SkipResults
            Get
                Return mSkipResults
            End Get
        End Property
        Friend Sub PluginManagerLookForHeaderTemplatesAndFinaliseEditSummary( _
        ByVal webcontrol As WikiFunctions.Browser.WebControl)
            Dim TalkHeaderParsingObject As TalkHeaderAndSkipTOCParser

            If mHavePlacedATemplateAtTop Then _
               TalkHeaderParsingObject = New TalkHeaderAndSkipTOCParser(AlteredArticleText)

            EditSummary = Regex.Replace(EditSummary, ", $", ".")
            webcontrol.SetMinor(Not mMajor)
        End Sub
        Friend Sub PluginManagerEditSummaryTaggingCategory(ByVal CategoryName As String)
            If Not CategoryName = "" Then EditSummary += "Tag [[Category:" + CategoryName + "]]. "
        End Sub

        ' General:
        Friend Sub DoneReplacement(ByVal Old As String, ByVal Replacement As String, _
        ByVal LogIt As Boolean, Optional ByVal PluginName As String = "")
            mProcessIt = True
            EditSummary += Old + "→" + Replacement + ", "
            If LogIt Then PluginSettingsControl.MyTrace.WriteArticleActionLine("Replacement: " + Old + "→" + _
               Replacement, PluginName)
        End Sub
        Friend Sub TemplateAdded(ByVal Template As String, ByVal PluginName As String)
            mEditSummary += String.Format("Added {{{{[[Template:{0}|{0}]]}}}}, ", Template)
            PluginSettingsControl.MyTrace.WriteTemplateAdded(Template, PluginName)
            ArticleHasAMajorChange()
        End Sub
        Friend Sub ParameterAdded(ByVal ParamName As String, ByVal ParamValue As String, ByVal PluginName As String)
            mEditSummary += ParamName & "=" & ParamValue & ", "
            PluginSettingsControl.MyTrace.WriteArticleActionLine(ParamName & "=" & ParamValue, PluginName)
            ArticleHasAMajorChange()
        End Sub
        Friend Sub RestoreTemplateToPlaceholderSpot(ByVal TemplateHeader As String)
            ' just write one instance of template even if have multiple conTemplatePlaceholder's
            Static strPlaceholder As String = Regex.Escape(PluginBase.conTemplatePlaceholder)
            Static RestoreTemplateToPlaceholderSpotRegex As New Regex(strPlaceholder)

            AlteredArticleText = RestoreTemplateToPlaceholderSpotRegex.Replace(AlteredArticleText, TemplateHeader, 1)
            AlteredArticleText = RestoreTemplateToPlaceholderSpotRegex.Replace(AlteredArticleText, "")
        End Sub
        'Friend Function ReplaceReqphotoWithTemplateParams(ByVal PluginName As String) As Boolean
        '    If ReqPhotoNoParamsRegex.IsMatch(AlteredArticleText) Then
        '        AlteredArticleText = ReqPhotoNoParamsRegex.Replace(AlteredArticleText, "")
        '        DoneReplacement("{{reqphoto}}", "template param(s)", True, PluginName)
        '        ArticleHasAMajorChange()
        '        Return True
        '    End If
        'End Function
        Friend Sub AddReqPhoto(ByVal PluginName As String)
            AlteredArticleTextPrependLine("{{reqphoto}}")
            ArticleHasAMajorChange()
            PluginSettingsControl.MyTrace.WriteArticleActionLine1("Added {{[[Template:Reqphoto|Reqphoto]]}}", PluginName, True)
        End Sub
        ' Why doesn't VB have these already? Seems too much to inherit from a String to add em, but these might help as is:
        Friend Sub AlteredArticleTextPrepend(ByVal Text As String)
            AlteredArticleText = Text + AlteredArticleText
        End Sub
        Friend Sub AlteredArticleTextPrependLine(ByVal Text As String)
            AlteredArticleText = Text + Microsoft.VisualBasic.vbCrLf + AlteredArticleText
        End Sub

        ''' <summary>
        ''' Private sealed class which parses for {{talkheader}} and {{skiptotoctalk}}
        ''' </summary>
        ''' <remarks></remarks>
        Private NotInheritable Class TalkHeaderAndSkipTOCParser
            Private Shared TalkheaderTemplateRegex As New Regex("\{\{\s*(template:)?talkheader\s*\}\}[\s\n\r]*", _
               RegexOptions.ExplicitCapture Or RegexOptions.Compiled Or RegexOptions.IgnoreCase)
            Private Shared SkipTOCTemplateRegex As New Regex( _
               "\{\{\s*(template:)?(skiptotoctalk|Skiptotoc|Skiptotoc-talk)\s*\}\}[\s\n\r]*", _
               RegexOptions.ExplicitCapture Or RegexOptions.Compiled Or RegexOptions.IgnoreCase)
            Friend FoundTalkheader As Boolean, FoundSkipTOC As Boolean

            Friend Sub New(ByRef ArticleText As String)
                ArticleText = TalkheaderTemplateRegex.Replace(ArticleText, AddressOf Me.MatchEvaluator, 1)
                ArticleText = SkipTOCTemplateRegex.Replace(ArticleText, AddressOf Me.MatchEvaluator2, 1)

                If FoundTalkheader Then WriteHeaderTemplate("talkheader", ArticleText)
                If FoundSkipTOC Then WriteHeaderTemplate("skiptotoctalk", ArticleText)
            End Sub

            Private Sub WriteHeaderTemplate(ByVal Name As String, ByRef ArticleText As String)
                ArticleText = "{{" & Name & "}}" & Microsoft.VisualBasic.vbCrLf & ArticleText
                PluginSettingsControl.MyTrace.WriteArticleActionLine1("{{tl|" & Name & " }} given top billing", _
                   "Plugin manager", True)
            End Sub
            Private Function MatchEvaluator(ByVal match As Match) As String
                FoundTalkheader = True
                Return ""
            End Function
            Private Function MatchEvaluator2(ByVal match As Match) As String
                FoundSkipTOC = True
                Return ""
            End Function
        End Class
    End Class

    ''' <summary>
    ''' An object which wraps around a collection of template parameters
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class Templating
        Friend FoundTemplate As Boolean = False, BadTemplate As Boolean = False
        Friend Parameters As New Dictionary(Of String, TemplateParametersObject)

        Friend Sub AddTemplateParmFromExistingTemplate(ByVal ParameterName As String, ByVal ParameterValue As String)
            If Parameters.ContainsKey(ParameterName) Then
                If Not Parameters.Item(ParameterName).Value = ParameterValue Then BadTemplate = True
            Else
                Parameters.Add(ParameterName, New TemplateParametersObject(ParameterName, ParameterValue))
            End If
        End Sub
        Friend Sub NewTemplateParm(ByVal ParameterName As String, ByVal ParameterValue As String)
            Parameters.Add(ParameterName, New TemplateParametersObject(ParameterName, ParameterValue))
        End Sub
        Friend Sub NewTemplateParm(ByVal ParameterName As String, ByVal ParameterValue As String, _
        ByVal LogItAndUpdateEditSummary As Boolean, ByVal TheArticle As Article, ByVal PluginName As String)
            Parameters.Add(ParameterName, New TemplateParametersObject(ParameterName, ParameterValue))
            If LogItAndUpdateEditSummary Then TheArticle.ParameterAdded(ParameterName, ParameterValue, PluginName)
        End Sub
        ''' <summary>
        ''' Checks for the existence of a parameter and adds it if missing/optionally changes it
        ''' </summary>
        ''' <returns>True if made a change</returns>
        Friend Function NewOrReplaceTemplateParm(ByVal ParameterName As String, ByVal ParameterValue As String, _
        ByVal TheArticle As Article, ByVal LogItAndUpdateEditSummary As Boolean, _
        ByVal ParamHasAlternativeName As Boolean, Optional ByVal DontChangeIfSet As Boolean = False, _
        Optional ByVal ParamAlternativeName As String = "", Optional ByVal PluginName As String = "") As Boolean

            If Parameters.ContainsKey(ParameterName) Then
                NewOrReplaceTemplateParm = ReplaceTemplateParm(ParameterName, ParameterValue, TheArticle, _
                   LogItAndUpdateEditSummary, DontChangeIfSet, PluginName)
            ElseIf ParamHasAlternativeName AndAlso Parameters.ContainsKey(ParamAlternativeName) Then
                NewOrReplaceTemplateParm = ReplaceTemplateParm(ParamAlternativeName, ParameterValue, TheArticle, _
                   LogItAndUpdateEditSummary, DontChangeIfSet, PluginName)
            Else ' Doesn't contain parameter
                NewTemplateParm(ParameterName, ParameterValue, LogItAndUpdateEditSummary, TheArticle, PluginName)
                NewOrReplaceTemplateParm = True
            End If

            If NewOrReplaceTemplateParm Then TheArticle.ArticleHasAMajorChange()
        End Function
        Private Function ReplaceTemplateParm(ByVal ParameterName As String, ByVal ParameterValue As String, _
        ByVal TheArticle As Article, ByVal LogItAndUpdateEditSummary As Boolean, ByVal DontChangeIfSet As Boolean, _
        ByVal PluginName As String) As Boolean
            Dim str As String = Parameters(ParameterName).Value

            If Not str = ParameterValue Then ' Contains parameter with a different value
                If str = "" OrElse Not DontChangeIfSet Then ' Contains parameter with a different value, and _
                    ' we want to change it, or contains an empty parameter
                    Parameters(ParameterName).Value = ParameterValue
                    TheArticle.ArticleHasAMajorChange()
                    If LogItAndUpdateEditSummary Then
                        If str = "" Then
                            TheArticle.ParameterAdded(ParameterName, ParameterValue, PluginName)
                        Else
                            TheArticle.DoneReplacement(str, ParameterValue, True, PluginName)
                        End If
                    End If
                    ReplaceTemplateParm = True
                Else ' Contains param with a different value, and we don't want to change it
                    PluginSettingsControl.MyTrace.WriteArticleActionLine( _
                       String.Format("{0} not changed, has existing value of {1}", _
                       ParameterName, ParameterValue), PluginName)
                End If
            End If ' Else: Already contains parameter and correct value; no need to change
        End Function
        Friend Sub ForceParentWorkgroupToNo(ByVal ChildWorkGroupParm As String, ByVal ParentWorkGroupParm As String, _
        ByVal Article As Article, ByVal PluginName As String)
            If Parameters.ContainsKey(ChildWorkGroupParm) AndAlso Parameters.ContainsKey(ParentWorkGroupParm) _
            AndAlso Parameters(ChildWorkGroupParm).Value = "yes" Then
                Parameters.Remove(ParentWorkGroupParm)
                Article.DoneReplacement(ParentWorkGroupParm, ChildWorkGroupParm, True, PluginName)
            End If
        End Sub

        ''' <summary>
        ''' An object which represents a template parameter
        ''' </summary>
        ''' <remarks></remarks>
        Friend NotInheritable Class TemplateParametersObject
            Public Name As String
            Public Value As String

            Friend Sub New(ByVal ParameterName As String, ByVal ParameterValue As String)
                Name = ParameterName
                Value = ParameterValue
            End Sub
        End Class
    End Class
End Namespace