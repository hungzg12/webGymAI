import {defineConfig} from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/postcss';
export default defineConfig({plugins:[react()],resolve:{preserveSymlinks:true,alias:{'@':process.cwd().replaceAll('\\','/')}},css:{postcss:{plugins:[tailwindcss()]}},server:{host:'127.0.0.1',port:3107,watch:{usePolling:true,interval:500}},build:{outDir:'dist'}});
