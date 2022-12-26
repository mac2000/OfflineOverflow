# macOS: ~/Library/Application\ Support/Google/Chrome/Default/History
# Windows: %LocalAppData%\Google\Chrome\User Data\Default\History
# Linux: ~/.config/google-chrome/Default/History

# copy sqlite database, otherwise can not run query, sqlite complains that database is locked, need to close chrome
cp '/Users/mac/Library/Application Support/Google/Chrome/Default/History' .

# ready stackoverflow urls from history
$urls = sqlite3 History "select url from urls where url like 'https://stackoverflow.com/questions/%'"
Write-Host "got $($urls.Count) urls from history"
# got 763 urls from history

# iterate over history, download pages, grab tags
$items = @()
foreach($url in $urls) {
    $html = Invoke-WebRequest $url | Select-Object -ExpandProperty Content
    $title = $html.Split('<title>')[1].Split('</title>')[0]
    $tags = $html.Split('js-post-tag-list-wrapper')[1].Split('</ul>')[0].Split('</a>') | ForEach-Object { $_.Split('>')[-1].Trim() } | Where-Object { $_ -ne '' -and $_ -ne $null }
    foreach($tag in $tags) {
        $items += [PSCustomObject]@{
            url = $url
            title = $title
            tag = $tag
        }
    }
    # to avoid rate limit
    Start-Sleep -Seconds 1
}

$items | Group-Object tag | Sort-Object Count -Descending | Where-Object count -ge 2 | Select-Object name
