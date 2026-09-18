import { useRef } from "react";
import ChatHistory from "./ChatHistory";
import Form, { type FormHandle } from "./Form";
import TextArea from "./TextArea";
import {
  addMessage,
  addMessageChunk,
  selectChatHistory,
  selectChatStatus,
  setStatus,
} from "../Slices/chatSlice";
import { useAppDispatch, useAppSelector } from "../Store/hooks";
import { postChatMessage, type Chunk } from "../Services/chatService";
import "./ChatComponent.css";

export default function ChatComponent() {
  const formRef = useRef<FormHandle>(null);
  const systemPromptRef = useRef<HTMLTextAreaElement>(null);
  const status = useAppSelector(selectChatStatus);
  const chatHistory = useAppSelector(selectChatHistory);
  const messagesEndRef = useRef<HTMLDivElement | null>(null);

  const dispatch = useAppDispatch();

  function handlePost(data: unknown) {
    const extractedData = data as {
      message: string;
    };
    const systemPrompt = systemPromptRef.current!.value;
    formRef.current!.clear();
    dispatch(setStatus("updating"));
    dispatch(
      addMessage({
        role: "User",
        content: extractedData.message,
        reference: crypto.randomUUID(),
      }),
    );

    postChatMessage(
      {
        systemPrompt: systemPrompt,
        messagePrompt: extractedData.message,
        chatHistory: [...chatHistory],
      },
      (chunk: Chunk) => {
        dispatch(
          addMessageChunk({ reference: chunk.Reference, chunk: chunk.Chunk }),
        );
        messagesEndRef.current!.scrollIntoView({ behavior: "smooth" });
      },
    ).finally(() => {
      dispatch(setStatus("idle"));
    });
  }
  function handleReset() {}
  return (
    <section className="chat-component">
      <div className="sidebar">
        <TextArea
          className="textarea-component"
          id="systemPrompt"
          label="System Prompt"
          ref={systemPromptRef}
        />
        <button onClick={handleReset}>Reset Chat</button>
      </div>
      <div className="main">
        <ChatHistory />
        <Form className="message-form" onPost={handlePost} ref={formRef}>
          <TextArea
            id="message"
            label="Message"
            className="textarea-component"
          />
          <button disabled={status === "updating"}>Post message</button>
        </Form>
        <div ref={messagesEndRef} className="end-of-message" />
      </div>
    </section>
  );
}
