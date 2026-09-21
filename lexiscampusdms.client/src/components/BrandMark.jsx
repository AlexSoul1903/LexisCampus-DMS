export default function BrandMark({ size = 20 }) {
    return (
        <svg width={size} height={size} viewBox="0 0 24 24" fill="none" aria-hidden>
            <path d="M12 2 2 7l10 5 10-5-10-5Z" fill="currentColor" opacity="0.9" />
            <path d="M5 10.2v5.3c0 .5.3 1 .8 1.2L12 20l6.2-3.3c.5-.2.8-.7.8-1.2v-5.3L12 13.8 5 10.2Z" fill="currentColor" opacity="0.55" />
            <path d="M21 7v6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
        </svg>
    );
}
