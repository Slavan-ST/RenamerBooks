from optimum.onnxruntime import ORTModelForFeatureExtraction
from transformers import AutoTokenizer
import os

# Указываем модель
model_id = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"
output_dir = "Assets"  # Папка в вашем C#-проекте

# Экспортируем ВСЮ модель (включая pooling!) в ONNX
print("Экспортирую модель в ONNX...")
ort_model = ORTModelForFeatureExtraction.from_pretrained(model_id, from_transformers=True)
tokenizer = AutoTokenizer.from_pretrained(model_id)

# Сохраняем
ort_model.save_pretrained(output_dir)
tokenizer.save_pretrained(output_dir)

print(f"✅ Модель успешно сохранена в: {os.path.abspath(output_dir)}")
print("Файлы: model.onnx, tokenizer.json, config.json и др.")