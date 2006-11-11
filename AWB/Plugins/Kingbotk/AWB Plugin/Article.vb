Namespace AWB.Plugins.SDKSoftware.Kingbotk
    ''' <summary>
    ''' An object representing an article which may or may not contain the targetted template
    ''' </summary>
    Friend NotInheritable Class Article
        ' Properties:
        Private mArticleText As String, mFullArticleTitle As String
        Private mArticleTitle As String, mNamespace As Namespaces, mHavePlacedATemplateAtTop As Boolean
        Private mEditSummary As String = PluginManager.conWikiPlugin, mMajor As Boolean
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

        ' Shared:
        Friend Shared Function GetURL(ByVal ArticleFullTitle As String) As String
            Return Variables.URL & "index.php?title=" & _
               System.Web.HttpUtility.UrlEncode(ArticleFullTitle.Replace(" ", "_"))
        End Function

        ' For calling by plugin:
        Friend Sub PluginCheckTemplateCall(ByVal TemplateCall As String, ByVal PluginName As String)
            If Not TemplateCall = "" Then ' we have "template:"
                mProcessIt = True
                EditSummary += "Remove ""template:"", "
                PluginSettingsControl.MyTrace.WriteArticleActionLine("Remove ""template:"" call", PluginName)
            End If
        End Sub
        Friend Sub PluginCheckTemplateName(ByVal TemplateName As String, ByVal PreferredTemplateNameRegex As Regex, _
        ByVal PreferredTemplateNameWiki As String, ByVal PluginName As String)
            If Not PreferredTemplateNameRegex Is Nothing Then
                If Not PreferredTemplateNameRegex.Match(TemplateName).Success Then
                    DoneReplacement(TemplateName, PreferredTemplateNameWiki, False)
                    PluginSettingsControl.MyTrace.WriteArticleActionLine( _
                       String.Format("Rename template [[Template:{0}|{0}]]→[[Template:{1}|{1}]]", TemplateName, _
                       PreferredTemplateNameWiki), PluginName)
                End If
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
        Friend Sub PluginManagerLookForTalkheaderTemplateAndFinaliseEditSummary( _
        ByVal webcontrol As WikiFunctions.Browser.WebControl)
            Dim TalkHeaderParsingObject As TalkHeaderParser

            If mHavePlacedATemplateAtTop Then
                TalkHeaderParsingObject = New TalkHeaderParser(AlteredArticleText)
                'If TalkHeaderParsingObject.FoundTemplate Then mEditSummary += _
                '   "moved {{[[Template:talkheader|talkheader]]}} to top, "
            End If

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

        Private NotInheritable Class TalkHeaderParser
            Private Shared TalkheaderTemplateRegex As New Regex("\{\{\s*(template:)?[tT]alkheader\s*\}\}", _
               RegexOptions.ExplicitCapture Or RegexOptions.Compiled)
            Friend FoundTemplate As Boolean

            Friend Sub New(ByRef ArticleText As String)
                ArticleText = TalkheaderTemplateRegex.Replace(ArticleText, AddressOf Me.MatchEvaluator, 1)
                If FoundTemplate Then
                    ArticleText = "{{talkheader}}" & Microsoft.VisualBasic.vbCrLf & ArticleText
                    PluginSettingsControl.MyTrace.WriteArticleActionLine1("{{tl|Talkheader}} given top slot", _
                       "Plugin manager", True)
                End If
            End Sub

            Private Function MatchEvaluator(ByVal match As Match) As String
                FoundTemplate = True
                Return ""
            End Function
        End Class
    End Class

    Friend NotInheritable Class Templating
        Friend FoundTemplate As Boolean = False, BadTemplate As Boolean = False
        Friend Parameters As New Dictionary(Of String, TemplateParametersObject)

        Friend Sub AddTemplateParmFromExistingTemplate(ByVal ParameterName As String, ByVal ParameterValue As String)
            Dim NewParm As New TemplateParametersObject(ParameterName, ParameterValue)

            If Parameters.ContainsKey(NewParm.Name) Then
                BadTemplate = True
            Else
                Parameters.Add(NewParm.Name, NewParm)
            End If
        End Sub
        Friend Sub NewTemplateParm(ByVal ParameterName As String, ByVal ParameterValue As String)
            Parameters.Add(ParameterName, New TemplateParametersObject(ParameterName, ParameterValue))
        End Sub
        ''' <summary>
        ''' Checks for the existence of a parameter and adds it if missing/optionally changes it
        ''' </summary>
        ''' <returns>True if made a change</returns>
        Friend Function NewOrReplaceTemplateParm(ByVal ParameterName As String, ByVal ParameterValue As String, _
        ByVal TheArticle As Article, ByVal LogItAndUpdateEditSummary As Boolean, _
        Optional ByVal PluginName As String = "", Optional ByVal DontChangeIfSet As Boolean = False) As Boolean
            Dim str As String

            If Parameters.ContainsKey(ParameterName) Then
                str = Parameters(ParameterName).Value
                If Not str = ParameterValue Then ' Contains parameter with a different value
                    If str = "" Or Not DontChangeIfSet Then ' Contains parameter with a different value, and _
                        ' we want to change it, or contains an empty parameter
                        Parameters(ParameterName).Value = ParameterValue
                        TheArticle.ArticleHasAMajorChange()
                        If LogItAndUpdateEditSummary
                            If str = "" Then
                                TheArticle.ParameterAdded(ParameterName, ParameterValue, PluginName)
                            Else
                                TheArticle.DoneReplacement(str, ParameterValue, True, PluginName)
                            End If
                        End If
                        NewOrReplaceTemplateParm = True
                    Else ' Contains param with a different value, and we don't want to change it
                        PluginSettingsControl.MyTrace.WriteArticleActionLine( _
                           String.Format("{0} not changed, has existing value of {1}", _
                           ParameterName, ParameterValue), PluginName)
                    End If
                End If ' Else: Already contains parameter and correct value; no need to change
            Else ' Doesn't contain parameter
                Parameters.Add(ParameterName, New TemplateParametersObject(ParameterName, ParameterValue))
                If LogItAndUpdateEditSummary Then _
                   TheArticle.ParameterAdded(ParameterName, ParameterValue, PluginName)
                NewOrReplaceTemplateParm = True
            End If

            If NewOrReplaceTemplateParm Then TheArticle.ArticleHasAMajorChange()
        End Function

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