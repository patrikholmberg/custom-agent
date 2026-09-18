import { configureStore, createSlice, type PayloadAction } from '@reduxjs/toolkit'
import { MarkdownConverter } from '@syncfusion/ej2-markdown-converter';
import type { RootState } from '../Store/store';

export type ChatStatus = 'updating' | 'idle';
export type AuthorRole = 'User' | 'Assistant';
export type MessageChunk = {
    reference: string;
    chunk: string;
}
export type Message = {
    reference: string;
    role: AuthorRole;
    content: string;
}
export type FormattedMessage = Message & {
    formattedMessage: string;
}
export interface ChatState{
    history: FormattedMessage[];
    status: ChatStatus;
}

const initialState: ChatState = {
    history:[],
    status: 'idle'
};

export const chatSlice = createSlice({
    name: "chat",
    initialState,
    reducers:{
        clearHistory: (state) => {
            state.history = [];
        },
        setStatus: (state, action: PayloadAction<ChatStatus>) => {
            state.status = action.payload;
        },
        addMessage: (state, action: PayloadAction<Message>) => {
            let formattedMessage = MarkdownConverter.toHtml(action.payload.content, {async: false}) as string;
            state.history = [...state.history, {...action.payload, formattedMessage: formattedMessage}];
        },
        addMessageChunk: (state, action: PayloadAction<MessageChunk>) => {
            if(state.status !== 'updating'){
                state.status = 'updating';
            }
            let message = state.history.find(item => item.reference === action.payload.reference);
            if(message !== undefined){
                message.content += action.payload.chunk;
                message.formattedMessage = MarkdownConverter.toHtml(message.content, {async: false}) as string;
            }else{
                message = {
                    reference: action.payload.reference,
                    content: action.payload.chunk,
                    role: 'Assistant',
                    formattedMessage: MarkdownConverter.toHtml(action.payload.chunk, {async: false}) as string
                }
            }
            state.history = [...state.history.filter(item => item.reference !== action.payload.reference), message];
        }
    }
});

export const { setStatus, addMessage, addMessageChunk, clearHistory } = chatSlice.actions;

export const chatReducer = chatSlice.reducer;

export const selectChatHistory = (state: RootState) => state.chatHistory.history;

export const selectChatStatus = (state: RootState) => state.chatHistory.status;