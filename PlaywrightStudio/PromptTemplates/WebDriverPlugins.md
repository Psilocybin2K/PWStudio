# WebDriver AI Tools Reference

## Navigation Tools

### GoToUrlAsync

**Purpose**: Navigate browser to specified URL

**Schema**:

```json
{
  "type": "function",
  "function": {
    "name": "GoToUrlAsync",
    "description": "Navigates the browser to the specified URL.",
    "parameters": {
      "type": "object",
      "properties": {
        "url": {
          "type": "string",
          "description": "The URL to navigate to."
        }
      },
      "required": ["url"]
    }
  }
}
```

**Input**: "Navigate to Google homepage"

**Example Call**:

```json
{
  "type": "function",
  "function": {
    "name": "GoToUrlAsync",
    "arguments": {
      "url": "https://google.com"
    }
  }
}
```

## Element Interaction Tools

### ClickAsync

**Purpose**: Click on specified element

**Schema**:

```json
{
  "type": "function",
  "function": {
    "name": "ClickAsync",
    "description": "Clicks the element specified by the CSS selector.",
    "parameters": {
      "type": "object",
      "properties": {
        "cssPath": {
          "type": "string",
          "description": "The CSS selector of the element to click."
        }
      },
      "required": ["cssPath"]
    }
  }
}
```

**Input**: "Click the submit button with CSS Path #submit-button"

**Example Call**:

```json
{
  "type": "function",
  "function": {
    "name": "ClickAsync",
    "arguments": {
      "cssPath": "#submit-button"
    }
  }
}
```

### DoubleClickAsync

**Purpose**: Double-click on specified element

**Schema**:

```json
{
  "type": "function",
  "function": {
    "name": "DoubleClickAsync",
    "description": "Double-clicks the element specified by the CSS selector.",
    "parameters": {
      "type": "object",
      "properties": {
        "cssPath": {
          "type": "string",
          "description": "The CSS selector of the element to double-click."
        }
      },
      "required": ["cssPath"]
    }
  }
}
```

**Input**: "Double-click the file item with CSS Path .file-item"

**Example Call**:

```json
{
  "type": "function",
  "function": {
    "name": "DoubleClickAsync",
    "arguments": {
      "cssPath": ".file-item"
    }
  }
}
```

### HoverAsync

**Purpose**: Hover mouse over specified element

**Schema**:

```json
{
  "type": "function",
  "function": {
    "name": "HoverAsync",
    "description": "Hovers the mouse over the element specified by the CSS selector.",
    "parameters": {
      "type": "object",
      "properties": {
        "cssPath": {
          "type": "string",
          "description": "The CSS selector of the element to hover over."
        }
      },
      "required": ["cssPath"]
    }
  }
}
```

**Input**: "Hover over the dropdown trigger with CSS Path .dropdown-trigger"

**Example Call**:

```json
{
  "type": "function",
  "function": {
    "name": "HoverAsync",
    "arguments": {
      "cssPath": ".dropdown-trigger"
    }
  }
}
```

## Input Tools

### EnterTextAsync

**Purpose**: Enter text into form fields or input elements

**Schema**:

```json
{
  "type": "function",
  "function": {
    "name": "EnterTextAsync",
    "description": "Enters text into the element specified by the CSS selector.",
    "parameters": {
      "type": "object",
      "properties": {
        "cssPath": {
          "type": "string",
          "description": "The CSS selector of the element to enter text into."
        },
        "text": {
          "type": "string",
          "description": "The text to enter."
        }
      },
      "required": ["cssPath", "text"]
    }
  }
}
```

**Input**: "Enter email address in username field with CSS Path input[name='username']"

**Example Call**:

```json
{
  "type": "function",
  "function": {
    "name": "EnterTextAsync",
    "arguments": {
      "cssPath": "input[name='username']",
      "text": "john.doe@mistral-nemo:7b"
    }
  }
}
```

### SendKeyAsync

**Purpose**: Send specific keyboard input to element

**Schema**:

```json
{
  "type": "function",
  "function": {
    "name": "SendKeyAsync",
    "description": "Sends a key to the element specified by the CSS selector.",
    "parameters": {
      "type": "object",
      "properties": {
        "key": {
          "type": "string",
          "description": "The key to send."
        },
        "cssPath": {
          "type": "string",
          "description": "The CSS selector of the element to send the key to."
        }
      },
      "required": ["key", "cssPath"]
    }
  }
}
```

**Input**: "Press Enter key on search field with CSS Path input[type='search']"

**Example Call**:

```json
{
  "type": "function",
  "function": {
    "name": "SendKeyAsync",
    "arguments": {
      "key": "Enter",
      "cssPath": "input[type='search']"
    }
  }
}
```

### SelectOptionAsync

**Purpose**: Select option from dropdown menus

**Schema**:

```json
{
  "type": "function",
  "function": {
    "name": "SelectOptionAsync",
    "description": "Selects an option from a dropdown element specified by the CSS selector.",
    "parameters": {
      "type": "object",
      "properties": {
        "cssPath": {
          "type": "string",
          "description": "The CSS selector of the dropdown element."
        },
        "option": {
          "type": "string",
          "description": "The option to select."
        }
      },
      "required": ["cssPath", "option"]
    }
  }
}
```

**Input**: "Select United States from country dropdown with CSS Path select[name='country']"

**Example Call**:

```json
{
  "type": "function",
  "function": {
    "name": "SelectOptionAsync",
    "arguments": {
      "cssPath": "select[name='country']",
      "option": "United States"
    }
  }
}
```

## Drag & Drop Tools

### DragToElementAsync

**Purpose**: Drag element from source to target location

**Schema**:

```json
{
  "type": "function",
  "function": {
    "name": "DragToElementAsync",
    "description": "Drags the element specified by the source CSS selector to the target CSS selector.",
    "parameters": {
      "type": "object",
      "properties": {
        "sourceCssPath": {
          "type": "string",
          "description": "The CSS selector of the source element to drag."
        },
        "targetCssPath": {
          "type": "string",
          "description": "The CSS selector of the target element to drop onto."
        }
      },
      "required": ["sourceCssPath", "targetCssPath"]
    }
  }
}
```

**Input**: "Drag item from CSS Path .draggable-item to drop zone with CSS Path .drop-zone"

**Example Call**:

```json
{
  "type": "function",
  "function": {
    "name": "DragToElementAsync",
    "arguments": {
      "sourceCssPath": ".draggable-item",
      "targetCssPath": ".drop-zone"
    }
  }
}
```

## Wait & Synchronization Tools

### WaitForElementVisibleAsync

**Purpose**: Wait until element becomes visible

**Schema**:

```json
{
  "type": "function",
  "function": {
    "name": "WaitForElementVisibleAsync",
    "description": "Waits until the element specified by the CSS selector is visible.",
    "parameters": {
      "type": "object",
      "properties": {
        "cssPath": {
          "type": "string",
          "description": "The CSS selector of the element to wait for."
        }
      },
      "required": ["cssPath"]
    }
  }
}
```

**Input**: "Wait for loading spinner to become visible with CSS Path .loading-spinner"

**Example Call**:

```json
{
  "type": "function",
  "function": {
    "name": "WaitForElementVisibleAsync",
    "arguments": {
      "cssPath": ".loading-spinner"
    }
  }
}
```

### WaitForElementHiddenAsync

**Purpose**: Wait until element becomes hidden

**Schema**:

```json
{
  "type": "function",
  "function": {
    "name": "WaitForElementHiddenAsync",
    "description": "Waits until the element specified by the CSS selector is hidden.",
    "parameters": {
      "type": "object",
      "properties": {
        "cssPath": {
          "type": "string",
          "description": "The CSS selector of the element to wait for."
        }
      },
      "required": ["cssPath"]
    }
  }
}
```

**Input**: "Wait for loading spinner to disappear with CSS Path .loading-spinner"

**Example Call**:

```json
{
  "type": "function",
  "function": {
    "name": "WaitForElementHiddenAsync",
    "arguments": {
      "cssPath": ".loading-spinner"
    }
  }
}
```

### WaitForUrlAsync

**Purpose**: Wait until an XHR request containing the URL has been made (API).

**Schema**:

```json
{
  "type": "function",
  "function": {
    "name": "WaitForUrlAsync",
    "description": "Waits until the page URL matches the specified URL.",
    "parameters": {
      "type": "object",
      "properties": {
        "url": {
          "type": "string",
          "description": "The URL to wait for."
        }
      },
      "required": ["url"]
    }
  }
}
```

**Input**: "Wait for URL to change to dashboard page"

**Example Call**:

```json
{
  "type": "function",
  "function": {
    "name": "WaitForUrlAsync",
    "arguments": {
      "url": "https://mistral-nemo:7b/dashboard"
    }
  }
}
```

### WaitForNetworkIdleAsync

**Purpose**: Wait until network activity stops

**Schema**:

```json
{
  "type": "function",
  "function": {
    "name": "WaitForNetworkIdleAsync",
    "description": "Waits until the network is idle.",
    "parameters": {
      "type": "object",
      "properties": {},
      "required": []
    }
  }
}
```

**Input**: "Wait for the page to finish loading"

**Example Call**:

```json
{
  "type": "function",
  "function": {
    "name": "WaitForNetworkIdleAsync",
    "arguments": {}
  }
}
```

## Resolving CSS Paths

### HasText

CSS Path should be transformed to `[text*='.. text ..']`.

**Examples**:

- Input: `button with "Login"`
- Output: `[text*='Login']`

**Tool Example**:

```json
{
  "type": "function",
  "function": {
    "name": "ClickAsync",
    "arguments": {
      "cssPath": "[text*='Login']"
    }
  }
}
```

- Input: `click the "Save Changes" button`
- Output: `[text*='Save Changes']`

**Tool Example**:

```json
{
  "type": "function",
  "function": {
    "name": "ClickAsync",
    "arguments": {
      "cssPath": "[text*='Save Changes']"
    }
  }
}
```

### HasTitle

CSS Path should be transformed to `[title*='.. title ..']`.

**Examples**:

- Input: `element with title "Submit Form"`
- Output: `[title*='Submit Form']`

**Tool Example**:

```json
{
  "type": "function",
  "function": {
    "name": "HoverAsync",
    "arguments": {
      "cssPath": "[title*='Submit Form']"
    }
  }
}
```

- Input: `click element titled "Close Window"`
- Output: `[title*='Close Window']`

**Tool Example**:

```json
{
  "type": "function",
  "function": {
    "name": "ClickAsync",
    "arguments": {
      "cssPath": "[title*='Close Window']"
    }
  }
}
```

### HasLabel

CSS Path should be transformed to `[label*='.. label ..']`.

**Examples**:

- Input: `by label "Username"`
- Output: `[label*='Username']`

**Tool Example**:

```json
{
  "type": "function",
  "function": {
    "name": "EnterTextAsync",
    "arguments": {
      "cssPath": "[label*='Username']",
      "text": "john.doe@mistral-nemo:7b"
    }
  }
}
```

- Input: `select "Country" dropdown`
- Output: `[label*='Country']`

**Tool Example**:

```json
{
  "type": "function",
  "function": {
    "name": "SelectOptionAsync",
    "arguments": {
      "cssPath": "[label*='Country']",
      "option": "United States"
    }
  }
}
```

---

Determine the applicable tools required to complete the task and invoke:

{{ $message }}