// Import the functions you need from the SDKs you need
import { initializeApp } from "firebase/app";
import { getAnalytics } from "firebase/analytics";
import { getAuth, GoogleAuthProvider } from "firebase/auth";
import { getFirestore, doc, setDoc } from "firebase/firestore";
// TODO: Add SDKs for Firebase products that you want to use
// https://firebase.google.com/docs/web/setup#available-libraries

// Your web app's Firebase configuration
// For Firebase JS SDK v7.20.0 and later, measurementId is optional
/*const firebaseConfig = {
  apiKey: "AIzaSyD5LSpLAoKdIuWlXM9XUPkGeuwPoWUGd44",
  authDomain: "financely-rec-a40a2.firebaseapp.com",
  projectId: "financely-rec-a40a2",
  storageBucket: "financely-rec-a40a2.appspot.com",
  messagingSenderId: "713491635578",
  appId: "1:713491635578:web:41538dff697a667aadcac3",
  measurementId: "G-CXXVR2F2VH"
};
*/

const firebaseConfig = {
  apiKey: "AIzaSyCOIcsPL0GLD35tcst2U1YysUV7UtSaoIc",
  authDomain: "my-pennypulse.firebaseapp.com",
  projectId: "my-pennypulse",
  storageBucket: "my-pennypulse.firebasestorage.app",
  messagingSenderId: "533980451129",
  appId: "1:533980451129:web:6256f3f76f7b6a6a2a58e8",
  measurementId: "G-ZG21FBD9F5"
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);
const analytics = getAnalytics(app);
const db = getFirestore(app);
const auth = getAuth(app);
const provider = new GoogleAuthProvider();
export { db, auth, provider, doc, setDoc };