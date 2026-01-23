@echo off
echo Starting FastAPI server...
python -m uvicorn main:app --reload
pause