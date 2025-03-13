/** @type {import('tailwindcss').Config} */
import PrimeUI from 'tailwindcss-primeui';

export default {
  content: [],
  theme: {
    extend: {
      fontFamily: {
        'noto-sans': ['Noto Sans JP', 'sans-serif'],
      }
    },
  },
  darkMode: ['selector', '[class~="dark-mode"]'],
  plugins: [PrimeUI],
}

