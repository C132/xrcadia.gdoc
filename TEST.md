# Markdown Viewer Test Document

This document exercises every Markdown feature a viewer should support. Use it as a visual regression test — open it in the Markdown Viewer and verify each section renders correctly.

---

## Inline Formatting

Plain text with **bold text** in the middle of a sentence.

Plain text with *italic text* in the middle of a sentence.

Plain text with ***bold and italic*** combined.

Plain text with `inline code` in the middle of a sentence.

Plain text with ~~strikethrough~~ in the middle of a sentence.

Mixing **bold with *nested italic* inside** a single phrase.

Mixing *italic with **nested bold** inside* a single phrase.

An **entire bold sentence with `inline code` inside it.**

Underscores also work: __bold__ and _italic_ and ___bold italic___.

Escaped special characters: \* \_ \` \\ \# \[ \] \( \) \{ \} \. \! \| \~ \>

---

## Headings

# Heading Level 1

## Heading Level 2

### Heading Level 3

#### Heading Level 4

##### Heading Level 5

###### Heading Level 6

### Heading with **bold** and *italic* and `code`

### Heading with [a link](https://example.com) inside

### Heading with ~~strikethrough~~ text

### 1\. Escaped Dot in Heading

### Heading with special chars: <angle> & "quotes"

---

## Paragraphs and Line Breaks

This is the first paragraph. It has multiple sentences. All sentences should flow together as a single block of text without any unexpected line breaks.

This is the second paragraph. There should be visible spacing between this and the paragraph above.

This line has two trailing spaces to force
a hard line break. This should appear on the next line within the same paragraph.

This line uses a backslash\
for a hard line break as well.

---

## Links

An inline link: [Example Website](https://example.com)

An inline link with title: [Example](https://example.com "Example Title")

A bare URL: https://example.com/path?query=value&other=true

A bare email: user@example.com

A reference-style link: [Reference Link][ref1]

A reference-style link with implicit label: [Google][]

[ref1]: https://example.com "Reference Example"
[Google]: https://google.com

---

## Images

An inline image:

![Alt text for test image](https://via.placeholder.com/400x200/333/fff?text=Inline+Image)

An image with title:

![Alt text](https://via.placeholder.com/300x150/555/fff?text=Image+With+Title "Image Title")

A reference-style image:

![Reference image][img1]

[img1]: https://via.placeholder.com/350x175/444/fff?text=Reference+Image

A linked image (image wrapped in a link):

[![Clickable image](https://via.placeholder.com/300x100/666/fff?text=Linked+Image)](https://example.com)

---

## Unordered Lists

- Item one
- Item two
- Item three

* Item with asterisk marker
* Second asterisk item

- First level
  - Second level
    - Third level
      - Fourth level
    - Back to third
  - Back to second
- Back to first

- List item with **bold** and *italic* and `code` formatting
- List item with [a link](https://example.com) inside
- List item that is long enough to wrap across multiple lines when the viewport is narrow, which tests that continuation lines are properly indented and aligned with the first line of content

---

## Ordered Lists

1. First item
2. Second item
3. Third item

1. Ordered with nesting
   1. Nested ordered
   2. Nested ordered
      1. Deeply nested
2. Back to top level

10. Starting at ten
11. Eleven
12. Twelve

1. Ordered with **bold**, *italic*, and `code`
2. Ordered with [a link](https://example.com)

---

## Mixed Lists

1. Ordered item
   - Unordered child
   - Another unordered child
2. Second ordered item
   - Child with sub-items
     1. Nested ordered in unordered
     2. Another nested ordered

- Unordered item
  1. Ordered child
  2. Another ordered child
- Another unordered item

---

## Task Lists

- [ ] Unchecked task
- [x] Checked task
- [ ] Another unchecked task with **bold text**
- [x] Checked task with `inline code`

---

## Blockquotes

> This is a single-line blockquote.

> This is a multi-line blockquote.
> It continues on the next line.
> And the next.

> This blockquote has **bold**, *italic*, and `code` formatting.

> First level quote
>
> > Nested quote
> >
> > > Deeply nested quote
>
> Back to first level

> ### Heading inside a blockquote
>
> Paragraph inside a blockquote with a [link](https://example.com).
>
> - List item inside a blockquote
> - Another list item

---

## Code Blocks

### Fenced with Backticks

```
Plain code block with no language specified.
It should render in a monospace font.
    Indentation should be preserved.
```

```csharp
// C# code block
using UnityEngine;

public class Example : MonoBehaviour
{
    [SerializeField] private float _speed = 5f;

    private void Update()
    {
        var direction = new Vector3(
            Input.GetAxis("Horizontal"),
            0f,
            Input.GetAxis("Vertical")
        );
        transform.position += direction * (_speed * Time.deltaTime);
    }
}
```

```python
# Python code block
def fibonacci(n: int) -> list[int]:
    """Generate the first n Fibonacci numbers."""
    if n <= 0:
        return []
    sequence = [0, 1]
    while len(sequence) < n:
        sequence.append(sequence[-1] + sequence[-2])
    return sequence[:n]

for i, num in enumerate(fibonacci(10)):
    print(f"F({i}) = {num}")
```

```json
{
  "name": "com.xrcadia.test",
  "version": "1.0.0",
  "displayName": "xrcadia.test",
  "description": "A test package.",
  "unity": "6000.3",
  "author": { "name": "xrcadia" },
  "keywords": ["test", "markdown"],
  "dependencies": {}
}
```

```javascript
// JavaScript with special characters
const encode = (s) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const msg = `Hello <world> & "friends"`;
console.log(encode(msg)); // Hello &lt;world&gt; &amp; &quot;friends&quot;
```

```
Code block with <angle brackets> & ampersands & "quotes"
These should not be interpreted as HTML.
Special: <b>not bold</b> <i>not italic</i>
```

### Fenced with Tildes

~~~
Code block using tilde fencing.
This is an alternative to backtick fencing.
~~~

### Indented Code Block

    This is an indented code block.
    Each line is indented by four spaces.
    It should render identically to fenced blocks.

---

## Tables

### Basic Table

| Column A | Column B | Column C |
|----------|----------|----------|
| Cell 1   | Cell 2   | Cell 3   |
| Cell 4   | Cell 5   | Cell 6   |
| Cell 7   | Cell 8   | Cell 9   |

### Aligned Table

| Left Aligned | Center Aligned | Right Aligned |
|:-------------|:--------------:|--------------:|
| Left         |    Center      |         Right |
| Data         |    Data        |          Data |
| Longer text  | Longer text    |   Longer text |

### Table with Formatting

| Feature        | Status   | Notes                          |
|----------------|----------|--------------------------------|
| **Bold**       | ✅ Done   | Renders correctly              |
| *Italic*       | ✅ Done   | Renders correctly              |
| `Code`         | ✅ Done   | Monospace styling              |
| [Link](https://example.com) | ✅ Done | Clickable |
| ~~Strike~~     | ⬜ Todo   | Not yet implemented            |

### Minimal Table

| A | B |
|---|---|
| 1 | 2 |

### Wide Table

| ID | Name | Email | Department | Role | Status | Start Date | Notes |
|----|------|-------|------------|------|--------|------------|-------|
| 1 | Alice | alice@example.com | Engineering | Lead | Active | 2024-01-15 | Founded team |
| 2 | Bob | bob@example.com | Engineering | Senior | Active | 2024-03-01 | Transferred from Design |
| 3 | Carol | carol@example.com | Design | Director | On Leave | 2023-06-20 | Returns March |

---

## Horizontal Rules

Three hyphens:

---

Three asterisks:

***

Three underscores:

___

---

## HTML Entities and Special Characters

HTML entities: &lt;div&gt; &amp; &quot;quoted&quot; &apos;apostrophe&apos;

Numeric entities: &#60;tag&#62; &#38; &#169; &#8212;

Unicode: em dash — en dash – ellipsis … bullet • copyright © trademark ™

Arrows: → ← ↑ ↓ ⇒ ⇐

Math symbols: ± × ÷ ≠ ≤ ≥ ∞ √ ∑ ∫ π

Emoji: 🎮 🚀 ✅ ❌ ⚙️ 📁 📄 🔧 💡 🎯

Currencies: $ € £ ¥ ₹ ₿

---

## Inline HTML

<p>A raw HTML paragraph tag.</p>

<div style="border: 1px solid #555; padding: 8px;">
  A styled div with <strong>strong</strong> and <em>emphasis</em>.
</div>

<details>
<summary>Click to expand</summary>

This content is inside a details/summary block.

- It can contain **markdown** formatting.
- And lists too.

</details>

<kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>P</kbd> keyboard shortcut styling.

Superscript: x<sup>2</sup> + y<sup>2</sup> = z<sup>2</sup>

Subscript: H<sub>2</sub>O is water.

<mark>Highlighted text</mark> using the mark tag.

---

## Footnotes

This sentence has a footnote[^1].

This one has another[^longnote].

[^1]: This is the first footnote content.
[^longnote]: This is a longer footnote with multiple paragraphs.

    The second paragraph of the footnote is indented.

---

## Definition Lists

Term 1
: Definition for term 1.

Term 2
: First definition for term 2.
: Second definition for term 2.

**Bold Term**
: Definition with *italic* and `code` elements.

---

## Abbreviations

The HTML specification is maintained by the W3C.

*[HTML]: Hyper Text Markup Language
*[W3C]: World Wide Web Consortium

---

## Math (LaTeX)

Inline math: $E = mc^2$

Display math:

$$
\frac{-b \pm \sqrt{b^2 - 4ac}}{2a}
$$

$$
\sum_{i=1}^{n} i = \frac{n(n+1)}{2}
$$

---

## Alerts / Admonitions (GitHub-style)

> [!NOTE]
> This is a note admonition. It provides additional context or information.

> [!TIP]
> This is a tip. It suggests a helpful practice or shortcut.

> [!IMPORTANT]
> This is important information that users should be aware of.

> [!WARNING]
> This is a warning. Proceed with caution.

> [!CAUTION]
> This is a caution. Potential danger ahead.

---

## Deeply Nested Content

1. Top-level ordered item
   - Unordered child
     > Blockquote inside a list
     >
     > With **formatted** content and `code`
   - Another child
     ```csharp
     // Code block inside a list
     Debug.Log("Hello from a nested code block");
     ```
2. Second top-level item
   1. Nested ordered
      - [ ] Task inside nested list
      - [x] Completed task inside nested list

> Blockquote containing:
>
> 1. An ordered list
> 2. With **bold** items
>    - And nested unordered items
>
> ```
> And a code block inside the blockquote
> ```
>
> | And | A | Table |
> |-----|---|-------|
> | In  | A | Quote |

---

## Edge Cases

### Empty Sections

### Consecutive Headings (no content between)

#### Sub-heading immediately after

### Very Long Heading That Goes On And On And Should Eventually Wrap To Multiple Lines In The Viewer To Test Heading Overflow Behavior

### Heading with only `code`

### **Fully Bold Heading**

### *Fully Italic Heading*

A paragraph with a very long unbroken word: Pneumonoultramicroscopicsilicovolcanoconiosis and then normal text continues after it.

A paragraph with a very long URL inline: https://example.com/very/long/path/that/goes/on/and/on/to/test/url/wrapping/behavior?with=query&params=too&and=more

### Trailing Whitespace and Special Lines

A line followed by an empty line:

And then this line.

Multiple empty lines above (should collapse to single spacing).



And this line after multiple blanks.

### Special Characters in Context

Pipes outside tables: | this | is | not | a | table |

Hashes not at line start: This is not ## a heading.

Backticks in prose: The command is `` `rm -rf` `` (double-backtick escape).

Asterisks in prose: 5 * 3 * 2 = 30 (should not become italic).

Underscores_in_words should not become italic.

An asterisk list marker right after a paragraph:
* This should be a list item
* And this too

---

## Paragraphs with Mixed Inline Elements

This paragraph has **bold**, *italic*, `code`, ~~strikethrough~~, [a link](https://example.com), and an image reference ![tiny](https://via.placeholder.com/16x16) all mixed together in a single flowing block of text to test how the viewer handles complex inline rendering without breaking layout.

Here is a paragraph with a footnote[^1], an abbreviation HTML, a `code span`, a **bold segment containing *nested italic* and `nested code`**, and a [link with **bold** inside](https://example.com) all in one go.

---

## End of Test Document

If every section above renders correctly — with proper styling, spacing, nesting, and no raw markdown syntax leaking through — the viewer fully supports the Markdown specification.
