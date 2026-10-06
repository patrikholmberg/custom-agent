import { selectChatHistory, selectChatStatus } from "../Slices/chatSlice";
import { useAppSelector } from "../Store/hooks";
import TypewriterMessage from "./TypewriterMessage";
import "./ChatHistory.css";

type ChatHistoryProps = {
  draft?: string;
  onReveal?: () => void;
};

export default function ChatHistory({ draft = "", onReveal }: ChatHistoryProps) {
  const chatHistory = useAppSelector(selectChatHistory);
  const status = useAppSelector(selectChatStatus);
  // While idle the terminal waits at a prompt echoing the message being typed;
  // while a reply streams, the cursor follows the output instead.
  const showPrompt = status === "idle";
  return (
    <div className="terminal">
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
                onReveal={onReveal}
              />
            </li>
          );
        })}
        {showPrompt && (
          <li className="user prompt">
            <div className="message">
              {draft}
              <span className="cursor"></span>
            </div>
          </li>
        )}
      </ul>
    </div>
  );
}
