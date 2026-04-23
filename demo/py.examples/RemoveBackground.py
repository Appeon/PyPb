from rembg import  remove, bg 
from PIL import Image
import sys

def removeBackground(source: str, destination: str):
    image = Image.open(source)
    output = remove(image)
    output.save(destination)

if __name__ == "__main__":
    bg.download_models(("u2net",))