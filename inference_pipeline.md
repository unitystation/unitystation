# Station AI Inference Pipeline

This document visualizes the exact, step-by-step path a user's chat message takes through the `ss13_dialogue` system to generate a lore-accurate response.

## Inference Pipeline Architecture

```
                         ┌─────────────────────┐
                         │  USER CHAT (String)  │
                         └──────────┬───────────┘
                                    │
                                    ▼
                           ┌────────────────┐
                           │   Flask API    │
                           └───────┬────────┘
                                   │
                                   ▼
                  ┌────────────────────────────────────────┐
                  │  Sentence-Transformers                 │
                  │  (BAAI/bge-small-en-v1.5)              │
                  └───────────────────┬────────────────────┘
                                      │
                                      ▼
                         ┌────────────────────────┐
                         │ Raw Embedding (384-D)  │
                         └───────────┬────────────┘
                                     │
                                     ▼
                            ┌────────────────┐
                            │   PCA Model    │
                            └───────┬────────┘
                                    │
                                    ▼
                   ┌──────────────────────────────┐
                   │  Reduced Embedding (64-D)    │
                   └───┬────────────┬─────────┬───┘
                       │            │         │
          ┌────────────┘            │         └───────────┐
          │                         │                     │
          ▼                         ▼                     ▼
┌───────────────────┐  ┌─────────────────────┐   (passed through
│ Cluster Distance  │  │  Leaky Reservoir    │    directly as
│   Calculator      │  │  RNN (512-D state,  │    64-D vector)
└────────┬──────────┘  │  random weights)    │         │
         │             └──────────┬──────────┘         │
         ▼                        ▼                    │
┌──────────────────┐  ┌─────────────────────┐          │
│ Distance Vector  │  │  Reservoir State    │          │
│   (18-D)         │  │  (512-D)            │          │
└────────┬─────────┘  └──────────┬──────────┘          │
         │                       │                     │
         └───────────┬───────────┴─────────────────────┘
                     │
                     ▼
        ┌──────────────────────────────────┐
        │  Concatenate Features            │
        │  (18 + 512 + 64 = 594-D)         │
        └───────────────┬──────────────────┘
                        │
                        ▼
        ┌──────────────────────────────────┐
        │  XGBoost Classifier              │
        │  (Trained Readout)               │
        └───────────────┬──────────────────┘
                        │
                        ▼
        ┌──────────────────────────────────┐
        │  Probability Array (18 classes)  │
        └───────────────┬──────────────────┘
                        │
                        ▼
              ┌─────────────────────┐
              │ Confidence ≥ 0.2 ?  │
              └───┬─────────────┬───┘
                  │             │
             YES  │             │  NO
                  ▼             ▼
  ┌──────────────────┐  ┌────────────────────────┐
  │ Anchor Qs for    │  │ Anchor Qs for          │
  │ Predicted Intent │  │ intent_chitchat        │
  └────────┬─────────┘  └───────────┬────────────┘
           │                        │
           └──────────┬─────────────┘
                      │
                      ▼
         ┌──────────────────────────────┐
         │ Cosine Similarity vs         │
         │ Anchor Embeddings            │
         └──────────────┬───────────────┘
                        │
                        ▼
         ┌──────────────────────────────┐
         │ Select Best-Matching         │
         │ Lore Response                │
         └──────────────┬───────────────┘
                        │
                        ▼
              ┌───────────────────┐
              │ JSON Response Dict│
              └───────────────────┘
```

## Step-by-Step Breakdown

1. **Input:** Accept raw text string from user.
2. **Embedding:** Encode via `BAAI/bge-small-en-v1.5` into a 384-D dense vector.
3. **PCA:** Reduce to `min(N, 64)` dimensions.
4. **Three parallel branches from the reduced embedding:**
   - **Cluster Distances:** Compute distances to the 18 cluster centers (Shape: 18).
   - **Reservoir State:** Feed into the Leaky Reservoir RNN. The reservoir has **randomly initialized, untrained weights**. It mixes the current input with fading echoes of previous inputs, producing a 512-D state vector.
   - **Raw Reduced Embedding:** Pass through directly (Shape: 64).
5. **Concatenation:** Merge all three into a single feature vector (18 + 512 + 64 = 594 dims).
6. **XGBoost (Trained Readout Layer):** This is the only trained component that reads the reservoir. It learns which random activations in the 512-D reservoir state correlate with specific conversational contexts, producing probabilities for all 18 intent classes.
7. **Thresholding:** If highest confidence < 0.2, fallback to `intent_chitchat`.
8. **Similarity Matching:** Look up anchor queries for the chosen intent, compute cosine similarity, pick the best match. (in other words, compare input with anchors, select best, spit response corresponding to the anchor). Idea here it's that when cluster is choosen by XGBOOST, cosine similarity will choose something sensible within that cluster even if other stuff from other clusters might be closer 
9. **Response Retrieval:** Return the lore response string for the best-matching anchor query.
