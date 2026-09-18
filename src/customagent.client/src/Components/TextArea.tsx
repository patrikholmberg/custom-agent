import type { ComponentPropsWithoutRef, RefObject } from "react";

type TextAreaProps = ComponentPropsWithoutRef<"textarea"> & {
  id: string;
  label: string;
  ref?: RefObject<HTMLTextAreaElement | null>;
};

export default function TextArea({ id, label, ref, ...props }: TextAreaProps) {
  return (
    <div className="input-component">
      <label htmlFor={id}>{label}</label>
      <textarea id={id} name={id} ref={ref} {...props} />
    </div>
  );
}
