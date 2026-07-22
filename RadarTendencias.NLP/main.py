from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from transformers import pipeline

app = FastAPI(title="Radar de Tendências - NLP Service")

try:
    sentiment_pipeline = pipeline(
        "sentiment-analysis",
        model="cardiffnlp/twitter-xlm-roberta-base-sentiment"
    )
except Exception:
    sentiment_pipeline = pipeline(
        "sentiment-analysis",
        model="distilbert-base-uncased-finetuned-sst-2-english"
    )


class TextRequest(BaseModel):
    text: str


class SentimentResponse(BaseModel):
    score: float
    label: str


@app.post("/analyze", response_model=SentimentResponse)
def analyze_sentiment(request: TextRequest):
    if not request.text or len(request.text.strip()) == 0:
        return SentimentResponse(score=50.0, label="neutral")

    try:
        result = sentiment_pipeline(
            request.text, truncation=True, max_length=512)[0]

        label = result['label'].lower()
        confidence = result['score']

        if "positive" in label:
            score = 50.0 + (confidence * 50.0)
        elif "negative" in label:
            score = 50.0 - (confidence * 50.0)
        else:
            score = 50.0

        return SentimentResponse(score=score, label=label)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=5000)
