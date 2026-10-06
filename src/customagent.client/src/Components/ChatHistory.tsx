import { useEffect, useRef } from "react";
import { selectChatHistory, selectChatStatus } from "../Slices/chatSlice";
import { useAppSelector } from "../Store/hooks";
import TerminalPrompt from "./TerminalPrompt";
import TypewriterMessage from "./TypewriterMessage";
import "./ChatHistory.css";

type ChatHistoryProps = {
  onSubmit: (message: string) => void;
};

// Keys that should land in the prompt even when it doesn't have focus.
function isTypingKey(e: KeyboardEvent) {
  if (e.altKey) {
    return false;
  }
  if (e.ctrlKey || e.metaKey) {
    return e.key.toLowerCase() === "v";
  }
  return e.key.length === 1 || e.key === "Backspace" || e.key === "Enter";
}

export default function ChatHistory({ onSubmit }: ChatHistoryProps) {
  const chatHistory = useAppSelector(selectChatHistory);
  const status = useAppSelector(selectChatStatus);
  const promptRef = useRef<HTMLTextAreaElement | null>(null);
  const endRef = useRef<HTMLDivElement | null>(null);
  // While idle the terminal waits at a prompt for the next message;
  // while a reply streams, the cursor follows the output instead.
  const showPrompt = status === "idle";

  useEffect(() => {
    // Moving focus during keydown makes the browser deliver the keystroke
    // (or paste) to the prompt, so typing anywhere ends up there.
    function handleKeyDown(e: KeyboardEvent) {
      const prompt = promptRef.current;
      if (prompt && document.activeElement !== prompt && isTypingKey(e)) {
        prompt.focus();
      }
    }
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  function handleReveal() {
    endRef.current?.scrollIntoView({ block: "end" });
  }

  function handleClick() {
    // Clicking anywhere in the terminal returns focus to the prompt, unless the
    // user is selecting text to copy.
    if (!window.getSelection()?.toString()) {
      promptRef.current?.focus();
    }
  }

  return (
    <div className="terminal" onClick={handleClick}>
      <ul className="chat-history">
        {chatHistory.map((message, index) => {
          return (
            <li
              key={message.reference}
              className={message.role === "User" ? "user" : "assistant"}
            >
              <TypewriterMessage
                content={message.content}
                formattedMessage={message.formattedMessage}
                animate={message.role === "Assistant" && status === "updating"}
                showCursor={!showPrompt && index === chatHistory.length - 1}
                onReveal={handleReveal}
              />
            </li>
          );
        })}
        {showPrompt && (
          <li className="user prompt">
            <div className="message">
              <TerminalPrompt
                ref={promptRef}
                onSubmit={onSubmit}
                onReveal={handleReveal}
              />
            </div>
          </li>
        )}
      </ul>
      <div ref={endRef} />
    </div>
  );
}
