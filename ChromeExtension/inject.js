
var pageElements = [];

// Note: Those variable are set by the caller code. See popup.html for their values
// var tags = ...
// var requestTabId = ...
// var parentTag = ...

// Simplify a html element by removing any outer elements that don't change text content
function shrinkElement(element) {
    if (element.childElementCount == 0) {
        return element;
    }

    // Don't shrink <code> blocks
    if (element.tagName == "CODE") {
        return element;
    }

    // One of the children elements has the same content as this element.
    // Discard the current element and use the child one.
    for (var i = 0; i < element.childElementCount; i++) {
        if (element.textContent == element.children[i].textContent) {
            return shrinkElement(element.children[i]);
        }
    }

    return element;
}

function translatePageElements() {
    var allElements = [];

    if (parentTag.length > 0) {
        parentElement = document.querySelector(parentTag);
        allElements = parentElement.querySelectorAll(tags);
    }
    else {
        allElements = document.querySelectorAll(tags);
    }

    console.log("TabId: " + requestTabId);

    // Note requestTabId is coming from popup.js along with injection
    for (var i = 0, l = allElements.length; i < l; i++)
    {
        console.log("Element index: " + i);
        newElement = shrinkElement(allElements[i]);
        console.log("Html: " + newElement.innerHTML + " tag: " + newElement.tagName);

        pageElements.push(newElement);

        // Don't translate <code> blocks
        if (newElement.tagName == "CODE") {
            continue;
        }

        if (pageElements[i].innerHTML.length == 0) {
            continue;
        }

        chrome.runtime.sendMessage({ tab: requestTabId, index: i, text: pageElements[i].innerHTML });

        // Mark text as pending translate: text -> [text]
        pageElements[i].innerHTML = "[" + pageElements[i].innerHTML + "]";
    }
}

chrome.runtime.onMessage.addListener(
    function(msg) {
        console.log(msg["index"]);
        console.log(msg["text"]);
        pageElements[msg["index"]].innerHTML = msg["text"];
    }
);

// Execute page translation on script injected
translatePageElements();
