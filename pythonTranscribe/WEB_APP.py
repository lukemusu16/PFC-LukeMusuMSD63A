import flask
from flask import Flask
import main

app = Flask(__name__)

@app.get("/")
def index():
    return main.srtGeneration(flask.request)

if __name__ == "__main__":
    app.run(host="localhost", port=8080)