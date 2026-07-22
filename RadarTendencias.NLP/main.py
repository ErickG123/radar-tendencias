from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from transformers import pipeline

app = FastAPI(title="Radar de Tendências - NLP Service")

sentiment_pipeline = pipeline(
    "sentiment-analysis", 
    model="nlptown/bert-base-multilingual-uncased-sentiment"
)

class TextRequest(BaseModel):
    text: str

class SentimentResponse(BaseModel):
    score: float
    label: str

@app.post("/analyze", response_model=SentimentResponse)
async def analyze_sentiment(request: TextRequest):
    if not request.text or len(request.text.strip()) == 0:
        return SentimentResponse(score=50.0, label="3 stars")
    
    try:
        truncated_text = request.text[:512]
        result = sentiment_pipeline(truncated_text)[0]
        
        stars = int(result['label'].split()[0])
        
        normalized_score = (stars / 5.0) * 100.0
        
        return SentimentResponse(score=normalized_score, label=result['label'])
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=5000)
