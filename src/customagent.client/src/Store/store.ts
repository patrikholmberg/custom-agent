import { configureStore, type Action, type ThunkAction } from "@reduxjs/toolkit";
import { chatReducer } from "../Slices/chatSlice";

export const store = configureStore({
    reducer:{
        chatHistory: chatReducer
    }
});

export type AppStore = typeof store;

export type RootState = ReturnType<AppStore["getState"]>;

export type AppDispatch = AppStore["dispatch"];

export type AppThunk<ThunkReturnType = void> = ThunkAction<ThunkReturnType, RootState, unknown, Action>;