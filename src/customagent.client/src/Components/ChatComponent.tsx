import { useState } from "react";
import ChatHistory from "./ChatHistory";
import {
  addMessage,
  addMessageChunk,
  clearHistory,
  selectChatHistory,
  setStatus,
} from "../Slices/chatSlice";
import { useAppDispatch, useAppSelector } from "../Store/hooks";
import { postChatMessage, type Chunk } from "../Services/chatService";
import "./ChatComponent.css";

export default function ChatComponent() {
  const chatHistory = useAppSelector(selectChatHistory);
  const [systemPrompt, setSystemPrompt] = useState("");

  const dispatch = useAppDispatch();

  function printLocal(command: string, output?: string) {
    dispatch(
      addMessage({
        role: "User",
        content: command,
        reference: crypto.randomUUID(),
        local: true,
      }),
    );
    if (output) {
      dispatch(
        addMessage({
          role: "Assistant",
          content: output,
          reference: crypto.randomUUID(),
          local: true,
        }),
      );
    }
  }

  // Commands start with a slash and are handled by the client, never sent to
  // the agent.
  function handleCommand(input: string) {
    const [, name, args = ""] = input.match(/^\/(\S*)\s*([\s\S]*)$/)!;
    switch (name.toLowerCase()) {
      case "clear":
        dispatch(clearHistory());
        break;
      case "systemprompt": {
        const prompt = args.trim();
        if (prompt) {
          setSystemPrompt(prompt);
          printLocal(input, "System prompt updated.");
        } else {
          printLocal(
            input,
            systemPrompt
              ? `Current system prompt:\n\n${systemPrompt}`
              : "No system prompt set. Use `/systemprompt <prompt>` to set one.",
          );
        }
        break;
      }
      default:
        printLocal(
          input,
          `Unknown command \`/${name}\`. Available commands: \`/systemprompt <prompt>\`, \`/clear\`.`,
        );
    }
  }

  function handlePost(message: string) {
    if (message.startsWith("/")) {
      handleCommand(message);
      return;
    }

    dispatch(setStatus("updating"));
    dispatch(
      addMessage({
        role: "User",
        content: message,
        reference: crypto.randomUUID(),
      }),
    );

    postChatMessage(
      {
        systemPrompt: systemPrompt,
        messagePrompt: message,
        chatHistory: chatHistory
          .filter((item) => !item.local)
          .map(({ role, content }) => ({ role, content })),
      },
      (chunk: Chunk) => {
        dispatch(
          addMessageChunk({ reference: chunk.Reference, chunk: chunk.Chunk }),
        );
      },
    ).finally(() => {
      dispatch(setStatus("idle"));
    });
  }

  return (
    <section className="chat-component">
      <ChatHistory onSubmit={handlePost} />
    </section>
  );
}
