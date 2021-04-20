//This is the document from popup.html

// Auto Select - Click event
document.getElementById('auto_select').addEventListener('click', function() {
    chrome.tabs.query({ active: true, currentWindow: true}, function(activeTabs) {
        var activeTabId = activeTabs[0].id;
        var code = "document.querySelector('article')";

        chrome.tabs.executeScript(activeTabId, { code }, function(results) {
            console.log(results);

            // If an 'article' tag exists, use it as our parent element
            if (results != null && results.length > 0 && results[0] != null) {
                document.getElementById('parent_tag').value = 'article';
            }
            else {
                document.getElementById('parent_tag').value = '';
            }
        });
    });
});

// Translate Page - Click event
document.getElementById('translate_page').addEventListener('click', function() {
    chrome.tabs.query({ active: true, currentWindow: true}, function(activeTabs) {
        var activeTabId = activeTabs[0].id;
        var tags = [];

        // Display checkboxes for html tags
        var allTagsStr = ["p", "h1", "h2", "h3", "h4", "h5", "li", "div", "td", "th"];
        for (var i = 0; i < allTagsStr.length; i++) {
            //console.log("index: " + i + " tag: " + allTagsStr[i]);
            if (document.getElementById("tag_" + allTagsStr[i]).checked) {
                tags.push(allTagsStr[i]);
            }
        }

        parentTag = '';
        parentTag = document.getElementById("parent_tag").value;

        checkedTagsStr = tags.join(',');
        chrome.tabs.executeScript(activeTabId, {
                code: 'var tags = ' + JSON.stringify(checkedTagsStr) +
                      '; var requestTabId = ' + activeTabId +
                      '; var parentTag = ' + JSON.stringify(parentTag) + ';'
            }, function() {
                // Now inject.js is running in the web page of the current tab
                chrome.tabs.executeScript(activeTabId, { file: "inject.js" }, function() {
                    if (chrome.runtime.lastError) {
                        console.error("Script injection failed: " + chrome.runtime.lastError.message);
                    }
            });
        });
    });
});

// Click 'Auto Select' on popup shows up
document.getElementById('auto_select').click();
