import requests
import re
from bs4 import BeautifulSoup

base_url = "https://github.com"
url = base_url + "/trending/python?since=monthly"

def retrievePage(url):
    return requests.get(url)
    

def retrievePosts():
    pageContents = retrievePage(url)
    soup = BeautifulSoup(pageContents.content, "html.parser")
    posts = [
         { "title": re.sub("\\n", "" , re.sub(" +", " ", post.find("h2").text)), 
       "description" : re.sub("\\n", "" , re.sub(" +", " ", post. find ("p").text)), 
       "url": base_url + post.find("h2").find("a").attrs['href'] 
       } for post in soup.find_all("article", class_="Box-row") ]
    return posts

if __name__ == "__main__":
    print(retrievePosts())