import {
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
  type KeyboardEvent,
  type RefObject,
} from "react";

type TerminalPromptProps = {
  onSubmit: (message: string) => void;
  onReveal?: () => void;
  ref?: RefObject<HTMLTextAreaElement | null>;
};

// The input line of the terminal. Enter sends the message, Shift+Enter adds a
// new line, and the textarea grows with its content like the rest of the output.
export default function TerminalPrompt({
  onSubmit,
  onReveal,
  ref,
}: TerminalPromptProps) {
  const [value, setValue] = useState("");
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);
  const onRevealRef = useRef(onReveal);

  useEffect(() => {
    onRevealRef.current = onReveal;
  }, [onReveal]);

  useLayoutEffect(() => {
    const textarea = textareaRef.current;
    if (!textarea) {
      return;
    }
    textarea.style.height = "auto";
    textarea.style.height = `${textarea.scrollHeight}px`;
    onRevealRef.current?.();
  }, [value]);

  function handleKeyDown(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key !== "Enter" || e.shiftKey || e.nativeEvent.isComposing) {
      return;
    }
    e.preventDefault();
    const message = value.trim();
    if (message) {
      onSubmit(message);
      setValue("");
    }
  }

  return (
    <textarea
      ref={(element) => {
        textareaRef.current = element;
        if (ref) {
          ref.current = element;
        }
      }}
      className="prompt-input"
      name="message"
      aria-label="Message"
      rows={1}
      spellCheck={false}
      autoFocus
      value={value}
      onChange={(e) => setValue(e.target.value)}
      onKeyDown={handleKeyDown}
    />
  );
}
