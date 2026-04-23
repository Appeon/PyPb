from openpyxl.styles import NamedStyle, PatternFill, Font, Color

def createDefaultStyle(styleName):
    style = NamedStyle(name=styleName)

    return style

def createStyle(styleName, fillColor, textColor):
    style = NamedStyle(name=styleName)
    color = Color(rgb=fillColor)
    font = Font(color=Color(rgb=textColor))
    style.fill = PatternFill(patternType='solid', bgColor=color, fgColor=color, fill_type='solid')
    style.font = font
    return style