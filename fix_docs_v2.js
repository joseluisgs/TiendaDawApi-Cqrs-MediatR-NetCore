const fs = require('fs');
const path = require('path');

const docDir = 'doc';
const files = fs.readdirSync(docDir).filter(f => f.endsWith('.md'));

files.forEach(file => {
    const filePath = path.join(docDir, file);
    let content = fs.readFileSync(filePath, 'utf8');
    
    // 1. Get target base number from filename (e.g. '01' -> '1', '29' -> '29')
    const fileNumMatch = file.match(/^(\d+)/);
    if (!fileNumMatch) return;
    const targetN = parseInt(fileNumMatch[1], 10).toString();
    const targetNAnchor = targetN; // Anchor version of base number (no dots)
    
    console.log(`Processing ${file} (Target N: ${targetN})`);
    
    // 2. Fix TOC links
    // Match [N_display.M. Text](#N_anchor.M_anchorRest)
    // Group 1: full display text
    // Group 2: number part in display text (e.g. 25.1)
    // Group 3: rest of display text
    // Group 4: number part in anchor (e.g. 251)
    // Group 5: rest of anchor
    const tocPattern = /\[((\d+(?:\.\d+)*)\.?\s+(.*?))\]\(#(\d+)(.*?)\)/g;
    
    let newContent = content.replace(tocPattern, (match, fullText, numPart, textAfterNum, oldAnchorNum, anchorRest) => {
        // Update numPart: replace the first segment with targetN
        const numSegments = numPart.split('.');
        numSegments[0] = targetN;
        const newNumPart = numSegments.join('.');
        
        // Update display text
        const newFullText = newNumPart + (numPart.includes('.') || fullText.includes('. ') ? '. ' : ' ') + textAfterNum;
        // Wait, the display text might already have the dot. Let's be careful.
        // Actually, let's just replace the number part.
        const updatedFullText = fullText.replace(numPart, newNumPart);
        
        // Update anchor number
        const newNumAnchor = newNumPart.replace(/\./g, '');
        
        return '[' + updatedFullText + '](#' + newNumAnchor + anchorRest + ')';
    });
    
    // 3. Fix Headers
    // Match line starting with #, followed by number
    // Group 1: hashes
    // Group 2: full number part
    // Group 3: rest of line
    const headerPattern = /^(#+)\s+(\d+(?:\.\d+)*)\.?\s+(.*)$/gm;
    
    newContent = newContent.replace(headerPattern, (match, hashes, numPart, restOfLine) => {
        const numSegments = numPart.split('.');
        if (numSegments[0] !== targetN) {
            numSegments[0] = targetN;
            const newNumPart = numSegments.join('.');
            return hashes + ' ' + newNumPart + '. ' + restOfLine;
        }
        return match;
    });
    
    if (newContent !== content) {
        fs.writeFileSync(filePath, newContent);
        console.log('  Updated ' + file);
    }
});
