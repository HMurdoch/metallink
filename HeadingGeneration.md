# Heading Generation Documentation

This document describes the process for generating metallic chrome-style heading images for the MetalLink application.

## Overview

All main system headings use metallic chrome-style PNG images with transparent backgrounds. These headings replace the old gradient text-based headings that used blue-to-grey gradients with animations.

## Current Headings

The following heading images are currently in use:

| Heading Text | File Name | Location | Used In |
|--------------|-----------|----------|---------|
| Metal Link | `metal_link_heading.png` | `MetalLink.Desktop/Assets/` | Login screen |
| Dashboard | `dashboard_heading.png` | `MetalLink.Desktop/Assets/` | Dashboard view |
| Customers | `customers_heading.png` | `MetalLink.Desktop/Assets/` | Customers view |
| Buyers | `buyers_heading.png` | `MetalLink.Desktop/Assets/` | Buyers view |
| Companies & Sites | `companies_and_sites_heading.png` | `MetalLink.Desktop/Assets/` | Companies & Sites view |
| Products | `products_heading.png` | `MetalLink.Desktop/Assets/` | Products view |
| Price Lists | `price_lists_heading.png` | `MetalLink.Desktop/Assets/` | Price Lists view |
| Prices | `prices_heading.png` | `MetalLink.Desktop/Assets/` | Prices view |
| Receiving | `receiving_heading.png` | `MetalLink.Desktop/Assets/` | Tickets Receiving view |
| Sending | `sending_heading.png` | `MetalLink.Desktop/Assets/` | Tickets Sending view |
| Stock Levels | `stock_levels_heading.png` | `MetalLink.Desktop/Assets/` | Stock Levels view |
| Stock Movements | `stock_movements_heading.png` | `MetalLink.Desktop/Assets/` | Stock Movements view |
| Reports | `reports_heading.png` | `MetalLink.Desktop/Assets/` | Reports view |
| Settings | `settings_heading.png` | `MetalLink.Desktop/Assets/` | Settings view |

## Visual Style

The headings feature:
- **Metallic chrome/silver gradient** with realistic light-to-dark banding
- **3D beveled edges** with highlights and shadows for depth
- **Drop shadow** for visual separation from background
- **Slight blue tint** for authentic chrome appearance
- **Bold italic font** (DejaVu Sans Bold Oblique or similar)
- **Transparent background** (PNG format) for flexibility

## Generation Process

### Prerequisites

- Python 3.x
- Pillow (PIL) library: `pip install Pillow`

### Generation Script

The heading generation script is located at the project root (created temporarily when needed):

```bash
python3 tmp_rovodev_generate_headings.py
```

This script:
1. Auto-sizes canvas to fit each heading text
2. Creates multiple layers for 3D chrome effect:
   - Shadow layer (bottom-right offset)
   - Dark outline/edge for bevel
   - Chrome gradient with vertical light bands
   - Top highlight edge for 3D effect
3. Applies image filters for smoothing and enhancement
4. Exports as PNG with transparency

### Manual Generation Script

If you need to regenerate headings or create new ones, use the following script:

```python
#!/usr/bin/env python3
"""
Generate metallic-style headings matching the example_heading.jpg style
Auto-sizes text and exports as PNG with transparent background
"""

from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageEnhance
import os

def create_metallic_heading(text, output_path):
    """
    Create a metallic/chrome style heading similar to the example.
    Auto-sizes the canvas to fit the text with proper padding.
    
    Args:
        text: The heading text to render
        output_path: Path where the PNG will be saved
    """
    # Try to find a bold italic font
    font_size = 120
    font = None
    
    # Try different font paths
    font_paths = [
        '/usr/share/fonts/truetype/dejavu/DejaVuSans-BoldOblique.ttf',
        '/usr/share/fonts/truetype/liberation/LiberationSans-BoldItalic.ttf',
        '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',
        '/System/Library/Fonts/Supplemental/Arial Bold Italic.ttf',
        'C:\\Windows\\Fonts\\arialbi.ttf',
    ]
    
    for font_path in font_paths:
        if os.path.exists(font_path):
            try:
                font = ImageFont.truetype(font_path, font_size)
                break
            except:
                continue
    
    if font is None:
        font = ImageFont.load_default()
    
    # Create a temporary image to measure text size
    temp_img = Image.new('RGBA', (2000, 500), (0, 0, 0, 0))
    temp_draw = ImageDraw.Draw(temp_img)
    bbox = temp_draw.textbbox((0, 0), text, font=font)
    text_width = bbox[2] - bbox[0]
    text_height = bbox[3] - bbox[1]
    
    # Add padding for shadow and effects
    padding_x = 40
    padding_y = 40
    shadow_offset = 12
    
    # Calculate canvas size with padding
    width = text_width + padding_x * 2 + shadow_offset
    height = text_height + padding_y * 2 + shadow_offset
    
    # Position text with padding
    x = padding_x
    y = padding_y - bbox[1]  # Adjust for text baseline
    
    # Create image with transparent background
    img = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    
    # Create a mask for the text
    mask = Image.new('L', (width, height), 0)
    mask_draw = ImageDraw.Draw(mask)
    mask_draw.text((x, y), text, font=font, fill=255)
    
    # Create the chrome gradient effect
    chrome_img = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    
    # Draw vertical gradient for chrome effect
    for py in range(height):
        if y <= py <= y + text_height:
            ratio = (py - y) / text_height
            
            # Create realistic chrome gradient with multiple bands
            if ratio < 0.15:
                intensity = int(240 - ratio * 400)
            elif ratio < 0.25:
                intensity = int(200 - (ratio - 0.15) * 1200)
            elif ratio < 0.35:
                intensity = int(80 + (ratio - 0.25) * 200)
            elif ratio < 0.50:
                intensity = int(100 - (ratio - 0.35) * 200)
            elif ratio < 0.65:
                intensity = int(70 + (ratio - 0.50) * 100)
            elif ratio < 0.80:
                intensity = int(85 - (ratio - 0.65) * 200)
            else:
                intensity = int(55 + (ratio - 0.80) * 400)
            
            intensity = max(0, min(255, intensity))
            
            # Create slight color variation for metallic look
            r = intensity
            g = intensity
            b = min(255, int(intensity * 1.08))  # Slight blue tint
            
            # Draw horizontal line
            for px in range(width):
                if mask.getpixel((px, py)) > 0:
                    chrome_img.putpixel((px, py), (r, g, b, 255))
    
    # Create shadow layer
    shadow = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    
    # Multiple shadow layers for depth
    for offset in range(shadow_offset, 0, -1):
        alpha = int(40 - (offset * 2))
        shadow_color = (20, 25, 30, alpha)
        shadow_draw.text((x + offset, y + offset), text, font=font, fill=shadow_color)
    
    # Create dark outline/edge
    outline = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    outline_draw = ImageDraw.Draw(outline)
    
    # Thick dark outline for bevel effect
    for dx in range(-4, 5):
        for dy in range(-4, 5):
            if dx*dx + dy*dy <= 16 and (dx != 0 or dy != 0):
                distance = (dx*dx + dy*dy) ** 0.5
                alpha = int(120 - distance * 20)
                outline_draw.text((x + dx, y + dy), text, font=font, fill=(25, 30, 35, alpha))
    
    # Create top highlight edge for 3D bevel
    highlight = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    highlight_draw = ImageDraw.Draw(highlight)
    highlight_draw.text((x - 2, y - 3), text, font=font, fill=(245, 250, 255, 180))
    highlight_draw.text((x - 1, y - 2), text, font=font, fill=(235, 240, 250, 140))
    
    # Composite all layers
    final = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    final = Image.alpha_composite(final, shadow)
    final = Image.alpha_composite(final, outline)
    final = Image.alpha_composite(final, chrome_img)
    final = Image.alpha_composite(final, highlight)
    
    # Apply slight blur for anti-aliasing
    final = final.filter(ImageFilter.SMOOTH_MORE)
    
    # Enhance contrast
    enhancer = ImageEnhance.Contrast(final)
    final = enhancer.enhance(1.4)
    
    # Enhance sharpness
    enhancer = ImageEnhance.Sharpness(final)
    final = enhancer.enhance(1.3)
    
    # Save as PNG with transparency
    final.save(output_path, 'PNG')
    print(f"Created: {output_path} ({width}x{height}px)")


# Example usage:
if __name__ == "__main__":
    # Create a single heading
    create_metallic_heading("New Heading", "new_heading.png")
    
    # Or generate all headings:
    headings = [
        "Metal Link",
        "Dashboard",
        "Customers",
        "Buyers",
        "Companies & Sites",
        "Products",
        "Price Lists",
        "Prices",
        "Receiving",
        "Sending",
        "Stock Levels",
        "Stock Movements",
        "Reports",
        "Settings"
    ]
    
    for heading in headings:
        filename = heading.lower().replace(" ", "_").replace("&", "and") + "_heading.png"
        create_metallic_heading(heading, filename)
```

## Usage in AXAML Files

Replace old gradient text headings with the new image headings:

### Old Style (Gradient Text with Animation)
```xml
<StackPanel Spacing="4">
  <Grid>
    <TextBlock Text="Dashboard" FontSize="22" FontWeight="Bold" Foreground="#f5f5f5" />
    <TextBlock Text="Dashboard" FontSize="22" FontWeight="Bold" Foreground="#4a9eff" Opacity="0">
      <!-- Animation keyframes... -->
    </TextBlock>
  </Grid>
  <Border Height="2">
    <Border.Background>
      <LinearGradientBrush>
        <!-- Gradient stops... -->
      </LinearGradientBrush>
    </Border.Background>
  </Border>
</StackPanel>
```

### New Style (PNG Image)
```xml
<Image Source="avares://MetalLink.Desktop/Assets/dashboard_heading.png"
       Height="40"
       HorizontalAlignment="Left"
       Margin="0,0,0,12"
       Stretch="Uniform" />
```

## Adding New Headings

To add a new heading:

1. **Generate the image:**
   ```python
   create_metallic_heading("New Section", "new_section_heading.png")
   ```

2. **Move to Assets:**
   ```bash
   mv new_section_heading.png MetalLink.Desktop/Assets/
   ```

3. **Use in AXAML:**
   ```xml
   <Image Source="avares://MetalLink.Desktop/Assets/new_section_heading.png"
          Height="40"
          HorizontalAlignment="Left"
          Margin="0,0,0,12"
          Stretch="Uniform" />
   ```

## Technical Details

### Image Specifications
- **Format:** PNG with transparency
- **Typical height:** 120-210px (auto-sized)
- **Typical width:** 500-1400px (auto-sized)
- **Display height:** 40px (scaled in UI)
- **Font size:** 120pt (source)
- **Color depth:** 32-bit RGBA

### Layer Composition
1. **Shadow Layer:** 12px offset, gradient alpha 40-2
2. **Outline Layer:** 4px radius, alpha 120-20
3. **Chrome Gradient:** Vertical bands with 7 transition zones
4. **Highlight Layer:** 2-3px offset, alpha 180-140

### Chrome Gradient Zones
- 0-15%: Top bright highlight (240 → lighter)
- 15-25%: Transition to darker
- 25-35%: Dark band
- 35-50%: Middle transition
- 50-65%: Lower dark area
- 65-80%: Lower highlight band
- 80-100%: Bottom edge highlight

## Maintenance

### Regenerating All Headings
If the style needs to be updated consistently:

```bash
python3 tmp_rovodev_generate_headings.py
mv generated_headings/*.png MetalLink.Desktop/Assets/
```

### Quality Checks
- Verify transparent background renders correctly
- Check contrast on both light and dark themes
- Ensure text is readable at 40px display height
- Validate chrome effect looks metallic

## Version History

- **2026-02-25:** Initial creation of metallic chrome headings
  - Replaced all old gradient text headings
  - Generated 12 heading images
  - Auto-sized canvas for each heading
  - PNG format with transparency

## References

- Original example: `example_heading.jpg`
- Generation script: `tmp_rovodev_generate_headings.py` (temporary)
- Assets location: `MetalLink.Desktop/Assets/`
- Views updated: All main system views (12 total)
