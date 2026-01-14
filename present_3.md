# 🍾 TerraWine — Update: Mini-Games + UI Prompts (Current State)

## ✅ מה התווסף / השתנה
בגרסה הנוכחית הוספנו תשתית למיני־גיימס כחלק מהחוויה של **מפת העולם** (World Map).
הפוקוס של העדכון הוא על משחקון חדש שבניתי:

### Bottle Mini-Game — “השלמת בקבוק”
- מיני־גיים מבוסס **Timing**: חלקי בקבוק נעים משמאל↔ימין, והמטרה היא ללחוץ **Space** כדי לעצור את החלק *בדיוק בתוך המסגרת*.
- יש **4 שלבים** (Stages), ובכל שלב המהירות עולה כדי להקשות.
- יש **טיימר** (30 שניות) + UI של “שלב נוכחי” + טקסט פידבק בתחתית.
- במקרה של פסילה / זמן שנגמר — מופעל אפקט ויזואלי שמדגיש כישלון (Screen Shake).

> ⚠️ הערה: כרגע המשחקון עדיין לא מחובר לזרימת המשחק הראשית (כניסה מסצנה אחרת / טריגר מהעולם) – זה בתכנון.

---

## 🧠 רעיון מפתח (Design)
לכל חלק בקבוק יש “Ghost” סטטי (חצי שקוף) שמסמן את המיקום הנכון שלו,
וב־Spawn פשוט מעתיקים את ה־Y של ה־Ghost אל החלק שזז — כך החלק תמיד מופיע בגובה הנכון בלי טבלת גבהים.

---

## 🧩 קבצי קוד מרכזיים

### 🎮 Bottle Mini-Game
- `MovingPiece.cs` — אחראי על תנועת החלק בציר X, עצירה, ודיווח האם יצא מהמסך (ה־Manager מחליט על השמדה/כישלון). :
  (https://github.com/Game-Development-Amit-and-Gal/TerraWine/blob/presentation_3/Assets/Scripts/BottleGame/MovingPiece.cs)
- `MiniGameManager` —  (https://github.com/Game-Development-Amit-and-Gal/TerraWine/blob/presentation_3/Assets/Scripts/Garden/BottleMiniGameManager.cs) 

### ✨ Visuals / Juice
- `MiniGameVisuals.cs` — שכבת ויזואל: Screen Shake לכישלון + Particles להצלחה (מופרד מהלוגיקה). :contentReference[oaicite:1]{index=1}
   (https://github.com/Game-Development-Amit-and-Gal/TerraWine/blob/presentation_3/Assets/Scripts/BottleGame/MiniGameVisuals.cs)


---
